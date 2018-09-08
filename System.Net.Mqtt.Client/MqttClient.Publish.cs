using System.Collections.Concurrent;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.QoSLevel;
using MqttPacketMap = System.Collections.Concurrent.ConcurrentDictionary<ushort, System.Net.Mqtt.MqttPacket>;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : IObservable<MqttMessage>
    {
        private readonly ConcurrentDictionary<IObserver<MqttMessage>, ObserverRemover> observers = new ConcurrentDictionary<IObserver<MqttMessage>, ObserverRemover>();
        private readonly MqttPacketMap pubMap = new MqttPacketMap();
        private readonly MqttPacketMap pubRecMap = new MqttPacketMap();
        private CancellationTokenSource dispatchCancellationSource;
        private ConcurrentQueue<MqttMessage> dispatchQueue;
        private SemaphoreSlim dispatchSemaphore;
        private Task dispatchTask;

        public IDisposable Subscribe(IObserver<MqttMessage> observer)
        {
            return observers.GetOrAdd(observer, o => new ObserverRemover(this, o));
        }

        public event MessageReceivedHandler MessageReceived;

        private void DispatchMessage(string topic, Memory<byte> payload)
        {
            dispatchQueue.Enqueue(new MqttMessage(topic, payload));
            dispatchSemaphore.Release();
        }

        private void StartDispatcher()
        {
            dispatchQueue = new ConcurrentQueue<MqttMessage>();
            dispatchCancellationSource = new CancellationTokenSource();
            dispatchSemaphore = new SemaphoreSlim(0);

            dispatchTask = Task.Run(() => StartDispatchWorkerAsync(dispatchCancellationSource.Token));
        }

        private async Task StopDispatchAsync()
        {
            dispatchCancellationSource.Cancel();
            dispatchCancellationSource.Dispose();
            dispatchCancellationSource = null;

            try
            {
                await dispatchTask.ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            dispatchQueue = null;
            dispatchSemaphore.Dispose();
            dispatchSemaphore = null;
        }

        private async Task StartDispatchWorkerAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                await dispatchSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if(dispatchQueue.TryDequeue(out var message))
                {
                    MessageReceived?.Invoke(this, message);

                    foreach(var observer in observers)
                    {
                        observer.Key.OnNext(message);
                    }
                }
            }
        }

        public async Task PublishAsync(string topic, Memory<byte> payload,
            QoSLevel qosLevel = AtMostOnce, bool retain = false, CancellationToken token = default)
        {
            CheckConnected();

            var packet = new PublishPacket(topic, payload) {QoSLevel = qosLevel, Retain = retain};

            if(qosLevel != AtMostOnce) packet.PacketId = idPool.Rent();

            await MqttSendPacketAsync(packet, token).ConfigureAwait(false);

            if(qosLevel == AtLeastOnce || qosLevel == ExactlyOnce)
            {
                pubMap.TryAdd(packet.PacketId, packet);
            }
        }

        private void Unsubscribe(IObserver<MqttMessage> observer)
        {
            observers.TryRemove(observer, out _);
        }

        private sealed class ObserverRemover : IDisposable
        {
            private readonly IObserver<MqttMessage> observer;
            private readonly MqttClient owner;

            public ObserverRemover(MqttClient owner, IObserver<MqttMessage> observer)
            {
                this.owner = owner;
                this.observer = observer;
            }

            public void Dispose()
            {
                owner.Unsubscribe(observer);
            }
        }
    }
}