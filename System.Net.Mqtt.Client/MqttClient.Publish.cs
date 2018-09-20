using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Client
{
    public delegate void MessageReceivedHandler(MqttClient sender, MqttMessage message);

    public partial class MqttClient : IObservable<MqttMessage>
    {
        private readonly HashQueue<ushort, MqttPacket> publishFlowPackets;
        private readonly ObserversContainer<MqttMessage> publishObservers;
        private readonly ConcurrentDictionary<ushort, MqttPacket> receiveFlowPackets;
        private CancellationTokenSource dispatchCancellationSource;
        private AsyncBlockingQueue<MqttMessage> dispatchQueue;
        private Task dispatchTask;

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
            dispatchQueue = new AsyncBlockingQueue<MqttMessage>();
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

            var packet = new PublishPacket(topic, payload) {QoSLevel = qosLevel, Retain = retain};

            if(qosLevel == AtLeastOnce || qosLevel == ExactlyOnce)
            {
                var packetId = packet.PacketId = idPool.Rent();

                var registered = publishFlowPackets.TryAdd(packetId, packet);

                Debug.Assert(registered, "Cannot register publish packet for QoS (L1,L2).");

                await MqttSendPacketAsync(packet, token).ConfigureAwait(false);
            }
            else
            {
                await MqttSendPacketAsync(packet, token).ConfigureAwait(false);
            }
        }

        private void OnPublishPacket(PublishPacket packet)
        {
            switch(packet.QoSLevel)
            {
                case AtLeastOnce:
                {
                    DispatchMessage(packet.Topic, packet.Payload);
                    var pubAckPacket = new PubAckPacket(packet.PacketId);
                    MqttSendPacketAsync(pubAckPacket);
                    break;
                }
                case ExactlyOnce:
                {
                    if(receiveFlowPackets.TryAdd(packet.PacketId, null))
                    {
                        DispatchMessage(packet.Topic, packet.Payload);
                    }

                    var pubRecPacket = new PubRecPacket(packet.PacketId);
                    MqttSendPacketAsync(pubRecPacket);
                    break;
                }
                case AtMostOnce:
                {
                    DispatchMessage(packet.Topic, packet.Payload);
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

        private void OnPubRelPacket(ushort packetId)
        {
            receiveFlowPackets.TryRemove(packetId, out _);
            MqttSendPacketAsync(new PubCompPacket(packetId));
        }

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

            MqttSendPacketAsync(pubRelPacket);
        }

        #endregion
    }
}