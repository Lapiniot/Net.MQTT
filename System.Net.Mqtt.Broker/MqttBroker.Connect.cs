using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Broker
{
    public sealed partial class MqttBroker
    {
        public void OnConnect(MqttBinaryProtocolHandler sender, ConnectPacket packet)
        {
            sender.SendAsync(new ConnAckPacket() { StatusCode = 0, SessionPresent = false });
        }

        public void OnDisconnect(MqttBinaryProtocolHandler sender)
        {
            throw new NotImplementedException();
        }

        public void OnPingReq(MqttBinaryProtocolHandler sender)
        {
            throw new NotImplementedException();
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