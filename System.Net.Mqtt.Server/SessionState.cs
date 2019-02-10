using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public abstract class SessionState : IDisposable
    {
        protected SessionState(string clientId, DateTime createdAt)
        {
            ClientId = clientId;
            CreatedAt = createdAt;
        }

        public string ClientId { get; }
        public DateTime CreatedAt { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {}

        #region Subscription state

        public abstract IDictionary<string, byte> GetSubscriptions();

        #endregion

        public virtual byte[] Subscribe((string filter, byte qosLevel)[] filters)
        {
            var length = filters.Length;

            var result = new byte[length];

            for(var i = 0; i < length; i++)
            {
                var (filter, qos) = filters[i];

                var value = qos;

                result[i] = AddTopicFilterCore(filter, value);
            }

            return result;
        }

        protected abstract byte AddTopicFilterCore(string filter, byte qos);

        public virtual void Unsubscribe(string[] filters)
        {
            foreach(var filter in filters)
            {
                RemoveTopicFilterCore(filter);
            }
        }

        protected abstract void RemoveTopicFilterCore(string filter);

        #region Incoming message delivery state

        public abstract ValueTask EnqueueAsync(Message message);
        public abstract ValueTask<Message> DequeueAsync(CancellationToken cancellationToken);

        #endregion
    }
}