using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    internal class MqttConnectionSession : AsyncConnectedObject, IMqttPacketServerHandler, IDisposable
    {
        private MqttBinaryProtocolHandler handler;

        public string ClientId { get; private set; }

        public MqttConnectionSession(INetworkTransport transport)
        {
            this.handler = new MqttBinaryProtocolHandler(transport, this);
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            return handler.ConnectAsync(cancellationToken);
        }

        protected override Task OnDisconnectAsync()
        {
            return handler.DisconnectAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                handler.Dispose();
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
            
            handler.SendConnAckAsync(0, false).ContinueWith(AcceptConnection);
        }

        private void AcceptConnection(Task task)
        {
            if(task.IsCompletedSuccessfully)
            {
                //pendingConnections.TryRemove(handler, out _);
                //sessions.TryAdd(handler.ClientId, new MqttSession(handler));
            }
        }

        private void AbortConnection(Task task)
        {
            _ = handler.DisconnectAsync();
        }

        void IMqttPacketServerHandler.OnDisconnect()
        {
            _ = handler.DisconnectAsync();
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
            throw new NotImplementedException();
        }

        void IMqttPacketServerHandler.OnUnsubscribe(UnsubscribePacket packet)
        {
            throw new NotImplementedException();
        }
    }
}