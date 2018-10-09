using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    public class MqttBroker : IDisposable
    {
        private readonly IPEndPoint endPoint;
        private readonly object syncRoot;
        private bool isListening;
        private Socket listener;

        public MqttBroker(IPEndPoint localEndPoint)
        {
            endPoint = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));
            syncRoot = new object();
        }

        public bool IsListening => isListening;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            if(!isListening)
            {
                lock(syncRoot)
                {
                    if(!isListening)
                    {
                        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        listener.Bind(endPoint);
                        listener.Listen(10);

                        Task.Run(StartConnectionListenerAsync);

                        isListening = true;
                    }
                }
            }
        }

        private async Task StartConnectionListenerAsync()
        {
            while(true)
            {
                var s = await listener.AcceptAsync().ConfigureAwait(false);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                listener?.Dispose();
            }
        }
    }
}