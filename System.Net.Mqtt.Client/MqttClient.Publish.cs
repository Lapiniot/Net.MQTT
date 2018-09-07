using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.QoSLevel;
using MqttPacketMap = System.Collections.Concurrent.ConcurrentDictionary<ushort, System.Net.Mqtt.MqttPacket>;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient : IObservable<MqttMessage>
    {
        private readonly MqttPacketMap pubMap = new MqttPacketMap();
        private readonly MqttPacketMap pubRecMap = new MqttPacketMap();
        private readonly SemaphoreSlim dispatchSemaphore = new SemaphoreSlim(0);
        private readonly new ConcurrentQueue<MqttMessage> dispatchQueue = new ConcurrentQueue<MqttMessage>();
        public event MessageReceivedHandler MessageReceived;
        private readonly ConcurrentDictionary<IObserver<MqttMessage>, ObserverRemover> observers = new ConcurrentDictionary<IObserver<MqttMessage>, ObserverRemover>();
        private CancellationTokenSource dispatchCancellationSource;
        private Task dispatchTask;

        private void DispatchMessage(string topic, Memory<byte> payload)
        {
            dispatchQueue.Enqueue(new MqttMessage(topic, payload));
            dispatchSemaphore.Release();
        }

        private void StartDispatcher()
        {
            dispatchCancellationSource = new CancellationTokenSource();
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

            var packet = new PublishPacket(topic, payload) { QoSLevel = qosLevel, Retain = retain };

            if(qosLevel != AtMostOnce) packet.PacketId = idPool.Rent();

            await MqttSendPacketAsync(packet, token).ConfigureAwait(false);

            if(qosLevel == AtLeastOnce || qosLevel == ExactlyOnce)
            {
                pubMap.TryAdd(packet.PacketId, packet);
            }
        }

        public IDisposable Subscribe(IObserver<MqttMessage> observer)
        {
            return observers.GetOrAdd(observer, o => new ObserverRemover(this, o));
        }

        private void Unsubscribe(IObserver<MqttMessage> observer)
        {
            observers.TryRemove(observer, out _);
        }

        private class ObserverRemover : IDisposable
        {
            private MqttClient owner;
            private IObserver<MqttMessage> observer;

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