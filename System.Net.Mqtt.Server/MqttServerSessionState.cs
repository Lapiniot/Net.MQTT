using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public abstract class MqttServerSessionState
    {
        protected MqttServerSessionState(string clientId, DateTime createdAt)
        {
            ClientId = clientId;
            CreatedAt = createdAt;
        }

        public string ClientId { get; }
        public DateTime CreatedAt { get; }

        #region Subscription state

        public abstract IReadOnlyDictionary<string, byte> GetSubscriptions();

        #endregion

        public virtual byte[] Subscribe((string filter, byte qosLevel)[] filters)
        {
            if(filters == null) throw new ArgumentNullException(nameof(filters));

            var length = filters.Length;

            var result = new byte[length];

            for(var i = 0; i < length; i++)
            {
                var (filter, qos) = filters[i];

                var value = qos;

                result[i] = AddTopicFilter(filter, value);
            }

            return result;
        }

        protected abstract byte AddTopicFilter(string filter, byte qos);

        public virtual void Unsubscribe(string[] filters)
        {
            if(filters == null) throw new ArgumentNullException(nameof(filters));

            foreach(var filter in filters) RemoveTopicFilter(filter);
        }

        protected abstract void RemoveTopicFilter(string filter);

        #region Incoming message delivery state

        public abstract ValueTask EnqueueAsync(Message message);

        public abstract ValueTask<Message> DequeueAsync(CancellationToken cancellationToken);

        #endregion
    }
}