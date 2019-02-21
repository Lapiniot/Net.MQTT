using System.Buffers;
using System.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private static readonly RawPacket PingPacket = new RawPacket(new byte[] {(byte)PingReq, 0});

        protected override void OnPingResp(byte header, ReadOnlySequence<byte> remainder)
        {
            Trace.WriteLine(DateTime.Now.TimeOfDay + ": Ping response from server");
        }

        private Task PingAsync(object state, CancellationToken cancellationToken)
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