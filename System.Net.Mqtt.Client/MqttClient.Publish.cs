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
        private readonly AsyncBlockingQueue<MqttMessage> dispatchQueue;
        private readonly HashQueue<ushort, MqttPacket> publishFlowPackets;
        private readonly ObserversContainer<MqttMessage> publishObservers;
        private readonly Dictionary<ushort, MqttPacket> receiveFlowPackets;
        private CancellationTokenSource dispatchCancellationSource;
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
            dispatchCancellationSource = new CancellationTokenSource();

            dispatchTask = Task.Run(() => StartDispatchWorkerAsync(dispatchCancellationSource.Token));
        }

        private async Task StopDispatcherAsync()
        {
            dispatchCancellationSource.Cancel();

            try
            {
                await dispatchTask.ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
            finally
            {
                dispatchCancellationSource.Dispose();
            }
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

            PublishPacket packet;

            if(qosLevel == AtLeastOnce || qosLevel == ExactlyOnce)
            {
                var id = idPool.Rent();

                packet = new PublishPacket(id, qosLevel, topic, payload, retain);

                var registered = publishFlowPackets.TryAdd(id, packet);

                Debug.Assert(registered, "Cannot register publish packet for QoS (L1,L2).");
            }
            else
            {
                packet = new PublishPacket(0, AtMostOnce, topic, payload, retain);
            }

            await MqttSendPacketAsync(packet, token).ConfigureAwait(false);
        }

        private void OnPublishPacket(PublishPacket packet)
        {
            switch(packet.QoSLevel)
            {
                case AtLeastOnce:
                {
                    DispatchMessage(packet.Topic, packet.Payload);
                    var pubAckPacket = new PubAckPacket(packet.Id);
                    MqttSendPacketAsync(pubAckPacket);
                    break;
                }
                case ExactlyOnce:
                {
                    if(receiveFlowPackets.TryAdd(packet.Id, null))
                    {
                        DispatchMessage(packet.Topic, packet.Payload);
                    }

                    var pubRecPacket = new PubRecPacket(packet.Id);
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
            receiveFlowPackets.Remove(packetId);
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