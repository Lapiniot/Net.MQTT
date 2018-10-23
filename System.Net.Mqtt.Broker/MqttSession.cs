using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    internal partial class MqttSession : AsyncConnectedObject, IMqttPacketServerHandler
    {
        private readonly MqttBroker broker;
        private readonly MqttBinaryProtocolHandler handler;
        private readonly INetworkTransport transport;

        internal MqttSession(INetworkTransport transport, MqttBroker broker)
        {
            this.transport = transport;
            this.broker = broker;
            handler = new MqttBinaryProtocolHandler(transport, this);
            idPool = new FastIdentityPool(1);
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
                    handler.SendConnAckAsync(0x02, false).ContinueWith(AbortConnection);
                }

                ClientId = Path.GetRandomFileName().Replace(".", "");
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
                broker.Join(this);
            }
        }

        private void AbortConnection(Task task)
        {
            var unused = DisconnectAsync();
        }
    }
}