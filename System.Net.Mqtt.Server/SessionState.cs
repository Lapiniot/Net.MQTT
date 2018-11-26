using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public abstract class SessionState : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        #region Subscription state

        public abstract IDictionary<string, byte> GetSubscriptions();
        public abstract byte[] Subscribe((string filter, byte qosLevel)[] filters);
        public abstract void Unsubscribe(string[] filters);

        #endregion

        #region Incoming message delivery state

        public abstract ValueTask EnqueueAsync(Message message);
        public abstract ValueTask<Message> DequeueAsync(CancellationToken cancellationToken);

        #endregion
    }
}