using System.Diagnostics;
using System.Linq;

namespace System.Net.Mqtt
{
    public static class MqttPacketExtensions
    {
        [Conditional("DEBUG")]
        public static void DebugDump(this MqttPacket packet)
        {
            Debug.WriteLine($"{{{string.Join(",", packet.GetBytes().ToArray().Select(b => "0x" + b.ToString("x2")))}}}");
        }
    }
}