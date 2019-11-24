using System.Buffers;
using System.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private static readonly PingPacket PingPacket = new PingPacket();

        protected override void OnPingResp(byte header, ReadOnlySequence<byte> remainder)
        {
            Trace.WriteLine(DateTime.Now.TimeOfDay + ": Ping response from server");
        }

        private Task PingAsync(CancellationToken cancellationToken)
        {
            Post(PingPacket);

            return Task.CompletedTask;
        }

        protected override void OnPacketSent()
        {
            pingWorker?.ResetDelay();
        }
    }
}