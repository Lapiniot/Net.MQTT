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
        private readonly MqttPacketMap pubMap = new MqttPacketMap();
        private readonly MqttPacketMap pubRecMap = new MqttPacketMap();
        private CancellationTokenSource dispatchCancellationSource;
        private ConcurrentQueue<MqttMessage> dispatchQueue;
        private SemaphoreSlim dispatchSemaphore;
        private Task dispatchTask;
        private ObserversContainer<MqttMessage> publishObservers = new ObserversContainer<MqttMessage>();

        IDisposable IObservable<MqttMessage>.Subscribe(IObserver<MqttMessage> observer)
        {
            return publishObservers.Subscribe(observer);
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
                    try
                    {
                        MessageReceived?.Invoke(this, message);
                    }
                    catch
                    {
                        //ignore
                    }

                    publishObservers.Notify(message);
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
    }
}