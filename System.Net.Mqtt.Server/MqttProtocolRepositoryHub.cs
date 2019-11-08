using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Extensions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttProtocolRepositoryHub<T> : MqttProtocolHub, ISessionStateRepository<T> where T : MqttServerSessionState
    {
        private readonly ILogger logger;
        private readonly ParallelOptions options;
        private readonly ConcurrentDictionary<string, T> states;
        private readonly int threshold;

        protected MqttProtocolRepositoryHub(ILogger logger, int maxDegreeOfParallelism = 4, int parallelMatchThreshold = 16)
        {
            this.logger = logger;
            threshold = parallelMatchThreshold;
            options = new ParallelOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism};
            states = new ConcurrentDictionary<string, T>();
        }

        [Conditional("TRACE")]
        protected void TraceMessage(string clientId, Message message)
        {
            var (topic, payload, qoSLevel, _) = message;
            logger.LogTrace("Outgoing message for '{0}' (Topic='{1}', Size={2}, QoS={3}", clientId, topic, payload.Length, qoSLevel);
        }

        #region Overrides of MqttProtocolRepositoryHub

        public override void DispatchMessage(Message message)
        {
            var (topic, payload, qos) = message;

            void DispatchCore(MqttServerSessionState state)
            {
                if(!TopicMatches(state.GetSubscriptions(), topic, out var level)) return;

                var adjustedQoS = Math.Min(qos, level);

                var msg = qos == adjustedQoS ? message : new Message(topic, payload, adjustedQoS, false);

                TraceMessage(state.ClientId, msg);

                var unused = state.EnqueueAsync(msg);
            }

            Parallel.ForEach(states.Values, options, DispatchCore);
        }

        public bool TopicMatches(IReadOnlyDictionary<string, byte> subscriptions, string topic, out byte qosLevel)
        {
            if(subscriptions == null) throw new ArgumentNullException(nameof(subscriptions));

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

                old.Dispose();
                return CreateState(id, true);
            }, cleanSession);

            existingSession = existing;
            return state;
        }

        protected abstract T CreateState(string clientId, bool clean);

        public void Remove(string clientId)
        {
            states.TryRemove(clientId, out var state);
            state?.Dispose();
        }

        #endregion
    }
}