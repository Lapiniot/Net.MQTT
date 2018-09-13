using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.QoSLevel;
using MqttPacketMap = System.Collections.Concurrent.ConcurrentDictionary<ushort, System.Net.Mqtt.MqttPacket>;

namespace System.Net.Mqtt.Client
{
    public delegate void MessageReceivedHandler(MqttClient sender, MqttMessage message);

    public partial class MqttClient : IObservable<MqttMessage>
    {
        private readonly MqttPacketMap publishFlowPackets = new MqttPacketMap();
        private readonly MqttPacketMap receiveFlowPackets = new MqttPacketMap();
        private readonly ConcurrentQueue<ushort> orderQueue = new ConcurrentQueue<ushort>();
        private CancellationTokenSource dispatchCancellationSource;
        private BlockingQueue<MqttMessage> dispatchQueue;
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
        }

        private void StartDispatcher()
        {
            dispatchQueue = new BlockingQueue<MqttMessage>();
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

            dispatchQueue.Dispose();
            dispatchQueue = null;
        }

        private async Task StartDispatchWorkerAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var (success, message) = await dispatchQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);

                if(success && message != null)
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

            var packet = new PublishPacket(topic, payload) { QoSLevel = qosLevel, Retain = retain };

            bool ensureDelivery = qosLevel == AtLeastOnce || qosLevel == ExactlyOnce;

            if(ensureDelivery)
            {
                var packetId = packet.PacketId = idPool.Rent();

                publishFlowPackets.TryAdd(packetId, packet);

                orderQueue.Enqueue(packetId);
            }

            await MqttSendPacketAsync(packet, token).ConfigureAwait(false);
        }

        private void OnPublishPacket(PublishPacket packet)
        {
            DispatchMessage(packet.Topic, packet.Payload);

            switch(packet.QoSLevel)
            {
                case AtLeastOnce:
                    {
                        var pubAckPacket = new PubAckPacket(packet.PacketId);
                        _ = MqttSendPacketAsync(pubAckPacket);
                        break;
                    }
                case ExactlyOnce:
                    {
                        var pubRecPacket = new PubRecPacket(packet.PacketId);
                        _ = MqttSendPacketAsync(pubRecPacket);
                        break;
                    }
            }
        }

        #region QoS Level 1

        private void OnPubAckPacket(ushort packetId)
        {
            if(publishFlowPackets.TryRemove(packetId, out _))
            {
                idPool.Return(packetId);
            }
        }

        #endregion

        #region QoS Level 2

        private void OnPubRelPacket(ushort packetId) => _ = MqttSendPacketAsync(new PubCompPacket(packetId));

        private void OnPubCompPacket(ushort packetId)
        {
            if(publishFlowPackets.TryRemove(packetId, out _))
            {
                idPool.Return(packetId);
            }
        }

        private void OnPubRecPacket(ushort packetId)
        {
            var pubRelPacket = new PubRelPacket(packetId);

            publishFlowPackets.AddOrUpdate(packetId, pubRelPacket, (id, _) => pubRelPacket);

            _ = MqttSendPacketAsync(pubRelPacket);
        }

        #endregion
    }
}