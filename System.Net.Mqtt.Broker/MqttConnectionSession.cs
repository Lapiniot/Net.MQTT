using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    internal class MqttConnectionSession : AsyncConnectedObject, IMqttPacketServerHandler, IDisposable
    {
        private MqttBinaryProtocolHandler handler;
        private readonly ConcurrentDictionary<string, QoSLevel> subscriptions;
        private readonly INetworkTransport transport;
        private readonly MqttBroker broker;
        public string ClientId { get; private set; }

        public MqttConnectionSession(INetworkTransport transport, MqttBroker broker)
        {
            this.transport = transport;
            this.broker = broker;
            this.handler = new MqttBinaryProtocolHandler(transport, this);
            subscriptions = new ConcurrentDictionary<string, QoSLevel>();
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

            for(int i = 0; i < packet.Topics.Count; i++)
            {
                var (topic, qos) = packet.Topics[i];
                subscriptions.AddOrUpdate(topic, qos, (_, __) => qos);
                result[i] = (byte)qos;
            }

            handler.SendSubAckAsync(packet.Id, result, default);
        }

        void IMqttPacketServerHandler.OnUnsubscribe(UnsubscribePacket packet)
        {
            foreach(var topic in packet.Topics)
            {
                subscriptions.TryRemove(topic, out _);
            }
        }
    }
}