using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private static readonly byte[] PingPacket = new byte[] { 0b1100_0000, 0b0000_0000 };

        protected override void OnPingResp(byte header, ReadOnlySequence<byte> reminder)
        {
        }

        private Task PingAsync(CancellationToken cancellationToken)
        {
            Post(PingPacket);

            return Task.CompletedTask;
        }

        protected override void OnPacketSent()
        {
            pinger?.ResetDelay();
        }
    }
}