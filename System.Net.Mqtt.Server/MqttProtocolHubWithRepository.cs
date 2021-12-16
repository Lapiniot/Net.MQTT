﻿using System.Collections.Concurrent;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;

using static System.Net.Mqtt.Packets.ConnAckPacket;
using static System.String;

namespace System.Net.Mqtt.Server;

internal record struct TopicComparerContext(string Topic)
{
    private int level = -1;

    public int Level => level;

    public void Accumulate(KeyValuePair<string, byte> pair)
    {
        var (filter, qos) = pair;
        if(MqttExtensions.TopicMatches(Topic, filter))
        {
            if(qos > level)
            {
                level = qos;
            }
        }
    }
}

public abstract partial class MqttProtocolHubWithRepository<T> : MqttProtocolHub, ISessionStateRepository<T>, IDisposable
    where T : MqttServerSessionState
{
    private readonly ILogger logger;
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly ConcurrentDictionary<string, T> states;
    private readonly int threshold;
    private bool disposed;

    protected ILogger Logger => logger;

    protected MqttProtocolHubWithRepository(ILogger logger, IMqttAuthenticationHandler authHandler,
        int parallelMatchThreshold = 16)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
        this.authHandler = authHandler;
        threshold = parallelMatchThreshold;
        states = new ConcurrentDictionary<string, T>();
    }

    #region Overrides of MqttProtocolHub

    public override async Task<MqttServerSession> AcceptConnectionAsync(NetworkTransport transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<MessageRequest> messageObserver,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var reader = transport.Reader;

        var rvt = MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken);
        var (_, _, _, buffer) = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.ConfigureAwait(false);

        try
        {
            if(ConnectPacket.TryRead(in buffer, out var connPack, out _))
            {
                var vvt = ValidateAsync(transport, connPack, cancellationToken);
                if(!vvt.IsCompletedSuccessfully)
                {
                    await vvt.ConfigureAwait(false);
                }

                if(authHandler?.Authenticate(connPack.UserName, connPack.Password) == false)
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, NotAuthorized }, cancellationToken).ConfigureAwait(false);
                    throw new AuthenticationException();
                }

                Message? willMessage = !IsNullOrEmpty(connPack.WillTopic)
                    ? new(connPack.WillTopic, connPack.WillMessage, connPack.WillQoS, connPack.WillRetain)
                    : null;

                return CreateSession(connPack, willMessage, transport, subscribeObserver, messageObserver);
            }
            else
            {
                throw new MissingConnectPacketException();
            }
        }
        finally
        {
            reader.AdvanceTo(buffer.End);
        }
    }

    protected abstract ValueTask ValidateAsync(NetworkTransport transport, ConnectPacket connectPacket, CancellationToken cancellationToken);

    protected abstract MqttServerSession CreateSession(ConnectPacket connectPacket, Message? willMessage, NetworkTransport transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<MessageRequest> messageObserver);

    public override Task DispatchMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var (topic, payload, qos, _) = message;

        ValueTask DispatchCoreAsync(T state, CancellationToken cancellationToken)
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

        return Parallel.ForEachAsync(states.Values, cancellationToken, (state, cancellationToken) => DispatchCoreAsync(state, cancellationToken));
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
        var context = new TopicComparerContext(topic);
        Parallel.ForEach(subscriptions, p => context.Accumulate(p));
        return context.Level;
    }

    private static int MatchSequential(IReadOnlyDictionary<string, byte> subscriptions, string topic)
    {
        var max = -1;
        foreach(var (filter, level) in subscriptions)
        {
            if(MqttExtensions.TopicMatches(topic, filter))
            {
                if(level > max)
                {
                    max = level;
                }
            }
        }
        return max;
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

    [LoggerMessage(17, LogLevel.Debug, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage")]
    private partial void LogOutgoingMessage(string clientId, string topic, int size, byte qos, bool retain);

    #region Implementation of IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if(disposed) return;

        if(disposing)
        {
            Parallel.ForEach(states.Values, state => (state as IDisposable)?.Dispose());
        }

        disposed = true;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}