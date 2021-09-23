using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;

using static System.Net.Mqtt.Packets.ConnAckPacket;
using static System.String;

namespace System.Net.Mqtt.Server
{
    public abstract partial class MqttProtocolHubWithRepository<T> : MqttProtocolHub, ISessionStateRepository<T> where T : MqttServerSessionState
    {
        private readonly ILogger logger;
        private readonly IMqttAuthenticationHandler authHandler;
        private readonly int connectTimeout;
        private readonly ConcurrentDictionary<string, T> states;
        private readonly int threshold;

        protected ILogger Logger => logger;

        protected MqttProtocolHubWithRepository(ILogger logger, IMqttAuthenticationHandler authHandler,
            int connectTimeout = 2000,
            int parallelMatchThreshold = 16)
        {
            ArgumentNullException.ThrowIfNull(logger);

            this.logger = logger;
            this.authHandler = authHandler;
            this.connectTimeout = connectTimeout;
            threshold = parallelMatchThreshold;
            states = new ConcurrentDictionary<string, T>();
        }

        #region Overrides of MqttProtocolHub

        public override async Task<MqttServerSession> AcceptConnectionAsync(
            NetworkTransport transport,
            IObserver<SubscriptionRequest> subscribeObserver,
            IObserver<MessageRequest> messageObserver,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(transport);

            var reader = transport.Reader;

            using var timeoutCts = new CancellationTokenSource(connectTimeout);
            using var jointCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var rt = MqttPacketHelpers.ReadPacketAsync(reader, timeoutCts.Token);
            var prr = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);

            reader.AdvanceTo(prr.Buffer.End);

            if(ConnectPacket.TryRead(prr.Buffer, out var connPack, out _))
            {
                var vt = ValidateAsync(transport, connPack, cancellationToken);
                if(!vt.IsCompletedSuccessfully)
                {
                    await vt.ConfigureAwait(false);
                }

                if(authHandler?.Authenticate(connPack.UserName, connPack.Password) == false)
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, NotAuthorized }, cancellationToken).ConfigureAwait(false);
                    throw new AuthenticationException();
                }

                var willMessage = !IsNullOrEmpty(connPack.WillTopic)
                    ? new Message(connPack.WillTopic, connPack.WillMessage, connPack.WillQoS, connPack.WillRetain)
                    : null;

                return CreateSession(connPack, willMessage, transport, subscribeObserver, messageObserver);
            }
            else
            {
                throw new MissingConnectPacketException();
            }
        }

        protected abstract ValueTask ValidateAsync(NetworkTransport transport, ConnectPacket connectPacket, CancellationToken cancellationToken);

        protected abstract MqttServerSession CreateSession(ConnectPacket connectPacket, Message willMessage, NetworkTransport transport,
            IObserver<SubscriptionRequest> subscribeObserver, IObserver<MessageRequest> messageObserver);

        public override ValueTask DispatchMessageAsync(Message message, CancellationToken cancellationToken)
        {
            var (topic, payload, qos) = message;

            ValueTask DispatchCoreAsync(MqttServerSessionState state, CancellationToken cancellationToken)
            {
                if(!TopicMatches(state.GetSubscriptions(), topic, out var level))
                {
                    return ValueTask.CompletedTask;
                }

                var adjustedQoS = Math.Min(qos, level);

                var msg = qos == adjustedQoS ? message : new Message(topic, payload, adjustedQoS, false);

                LogOutgoingMessage(state.ClientId, topic, payload.Length, adjustedQoS, false);

                return state.EnqueueAsync(msg, cancellationToken);
            }

            return new ValueTask(Parallel.ForEachAsync(states.Values, cancellationToken, DispatchCoreAsync));
        }

        public bool TopicMatches(IReadOnlyDictionary<string, byte> subscriptions, string topic, out byte qosLevel)
        {
            ArgumentNullException.ThrowIfNull(subscriptions);

            var topQoS = subscriptions.Count > threshold ? MatchParallel(subscriptions, topic) : MatchSequential(subscriptions, topic);

            if(topQoS >= 0)
            {
                qosLevel = (byte)topQoS;
                return true;
            }

            qosLevel = 0;
            return false;
        }

        private static int MatchParallel(IReadOnlyDictionary<string, byte> subscriptions, string topic)
        {
            return subscriptions.AsParallel().Where(s => MqttExtensions.TopicMatches(topic, s.Key)).Aggregate(-1, Max);
        }

        private static int MatchSequential(IReadOnlyDictionary<string, byte> subscriptions, string topic)
        {
            return subscriptions.Where(s => MqttExtensions.TopicMatches(topic, s.Key)).Aggregate(-1, Max);
        }

        private static int Max(int acc, KeyValuePair<string, byte> current)
        {
            return Math.Max(acc, current.Value);
        }

        #endregion

        #region Implementation of ISessionStateRepository<out T>

        public T GetOrCreate(string clientId, bool cleanSession, out bool existingSession)
        {
            var existing = false;
            var state = states.AddOrUpdate(clientId, CreateState, (id, old, clean) =>
            {
                if(!clean)
                {
                    existing = true;
                    return old;
                }

                (old as IDisposable)?.Dispose();
                return CreateState(id, true);
            }, cleanSession);

            existingSession = existing;
            return state;
        }

        protected abstract T CreateState(string clientId, bool clean);

        public void Remove(string clientId)
        {
            states.TryRemove(clientId, out var state);
            (state as IDisposable)?.Dispose();
        }

        #endregion

        [LoggerMessage(17, LogLevel.Information, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage")]
        private partial void LogOutgoingMessage(string clientId, string topic, int size, byte qos, bool retain);
    }
}