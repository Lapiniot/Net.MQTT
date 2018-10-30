using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    internal partial class MqttSession : AsyncConnectedObject, IMqttPacketServerHandler
    {
        private readonly MqttBinaryProtocolHandler handler;
        private readonly MqttServer server;
        private readonly INetworkTransport transport;

        internal MqttSession(INetworkTransport transport, MqttServer server)
        {
            this.transport = transport;
            this.server = server;
            handler = new MqttBinaryProtocolHandler(transport, this);
            idPool = new FastPacketIdPool();
            receivedQos2 = new ConcurrentDictionary<ushort, bool>();
            subscriptions = new ConcurrentDictionary<string, byte>();
            resendQueue = new HashQueue<ushort, MqttPacket>();
        }

        public string ClientId { get; private set; }

        void IMqttPacketServerHandler.OnConnect(ConnectPacket packet)
        {
            if(packet.ClientId == null)
            {
                if(!packet.CleanSession)
                {
                    RejectConnection(0x02);
                }

                ClientId = Path.GetRandomFileName().Replace(".", "");
            }

            // if(packet.ProtocolLevel != 0x04 || packet.ProtocolName != "MQTT")
            // {
            //     RejectConnection(0x01);
            // }

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

        private void RejectConnection(byte statusCode)
        {
            handler.SendConnAckAsync(statusCode, false).ContinueWith(AbortConnection);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                handler.Dispose();
                transport.Dispose();
                resendQueue.Dispose();
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

        private void AcceptConnection(Task task)
        {
            if(task.IsCompletedSuccessfully)
            {
                server.Join(this);
            }
        }

        private void AbortConnection(Task task)
        {
            var unused = DisconnectAsync();
        }
    }
}