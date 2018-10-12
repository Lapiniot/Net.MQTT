using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    public sealed partial class MqttBroker
    {
        public void OnConnect(MqttBinaryProtocolHandler sender, ConnectPacket packet)
        {
            sender.SendConnAckAsync(0, false);
        }

        public void OnDisconnect(MqttBinaryProtocolHandler sender)
        {
            _ = sender.DisconnectAsync();
        }

        public void OnPingReq(MqttBinaryProtocolHandler sender)
        {
            sender.SendPingRespAsync();
        }

        private async Task StartAcceptingConnectionsAsync(IConnectionListener listener, CancellationToken cancellationToken)
        {
            listener.Start();

            while(!cancellationToken.IsCancellationRequested)
            {
                var transport = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);

                var handler = new MqttBinaryProtocolHandler(transport, this);

                cancellationToken.ThrowIfCancellationRequested();

                await handler.ConnectAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}