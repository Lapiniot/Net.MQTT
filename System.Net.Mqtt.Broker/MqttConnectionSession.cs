using System.Collections.Concurrent;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    internal class MqttConnectionSession : AsyncConnectedObject, IMqttPacketServerHandler
    {
        private readonly MqttBroker broker;
        private readonly MqttBinaryProtocolHandler handler;
        private readonly ConcurrentDictionary<string, QoSLevel> subscriptions;
        private readonly INetworkTransport transport;

        internal MqttConnectionSession(INetworkTransport transport, MqttBroker broker)
        {
            this.transport = transport;
            this.broker = broker;
            handler = new MqttBinaryProtocolHandler(transport, this);
            subscriptions = new ConcurrentDictionary<string, QoSLevel>();
        }

        public string ClientId { get; private set; }

        void IMqttPacketServerHandler.OnConnect(ConnectPacket packet)
        {
            if(packet.ClientId == null)
            {
                if(!packet.CleanSession)
                {
                    handler.SendConnAckAsync(0x02, false).ContinueWith(AbortConnection);
                }

                ClientId = Path.GetRandomFileName().Replace(".", "-");
            }

            ClientId = packet.ClientId;

            handler.SendConnAckAsync(0, false).ContinueWith(AcceptConnection);
        }

        void IMqttPacketServerHandler.OnDisconnect()
        {
            transport.DisconnectAsync();
        }

        void IMqttPacketServerHandler.OnPingReq()
        {
            handler.SendPingRespAsync();
        }

        void IMqttPacketServerHandler.OnPubAck(PubAckPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPubComp(PubCompPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPublish(PublishPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPubRec(PubRecPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnPubRel(PubRelPacket packet)
        {
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnSubscribe(SubscribePacket packet)
        {
            var result = new byte[packet.Topics.Count];

            for(var i = 0; i < packet.Topics.Count; i++)
            {
                var (topic, qos) = packet.Topics[i];
                subscriptions.AddOrUpdate(topic, qos, (_, __) => qos);
                result[i] = (byte)qos;
            }

            handler.SendSubAckAsync(packet.Id, result);
        }

        void IMqttPacketServerHandler.OnUnsubscribe(UnsubscribePacket packet)
        {
            foreach(var topic in packet.Topics)
            {
                subscriptions.TryRemove(topic, out _);
            }
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            return handler.ConnectAsync(cancellationToken);
        }

        protected override async Task OnDisconnectAsync()
        {
            await handler.DisconnectAsync().ConfigureAwait(false);
            await transport.DisconnectAsync().ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                handler.Dispose();
                transport.Dispose();
            }
        }

        private void AcceptConnection(Task task)
        {
            if(task.IsCompletedSuccessfully)
            {
                broker.AcceptSession(this);
            }
        }

        private void AbortConnection(Task task)
        {
            transport.DisconnectAsync();
        }
    }
}