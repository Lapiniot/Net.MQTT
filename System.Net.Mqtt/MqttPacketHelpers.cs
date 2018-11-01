using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public static class MqttPacketHelpers
    {
        [Conditional("DEBUG")]
        public static void DebugDump(this MqttPacket packet)
        {
            Debug.WriteLine($"{{{string.Join(",", packet.GetBytes().ToArray().Select(b => "0x" + b.ToString("x2")))}}}");
        }

        public static async Task<ReadResult> ReadPacketAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            while(true)
            {
                var task = reader.ReadAsync(cancellationToken);
                var result = task.IsCompleted ? task.Result : await task.ConfigureAwait(false);
                var buffer = result.Buffer;

                if(TryParseHeader(buffer, out var flags, out var length, out var offset))
                {
                    if(buffer.Length < offset + length)
                    {
                        // Not enough data received yet
                        continue;
                    }

                    return new ReadResult(flags, offset, length, buffer.Slice(0, offset + length));
                }

                if(buffer.Length >= 5)
                {
                    // We must stop here, because no valid MQTT packet header
                    // was found within 5 (max possible header size) bytes
                    throw new InvalidDataException(PacketDataExpected);
                }
            }
        }
    }

    public struct ReadResult
    {
        public byte Flags { get; }
        public int Offset { get; }
        public int Length { get; }
        public ReadOnlySequence<byte> Buffer { get; }

        public ReadResult(byte flags, int offset, int length, ReadOnlySequence<byte> buffer)
        {
            Flags = flags;
            Offset = offset;
            Length = length;
            Buffer = buffer;
        }

        public void Deconstruct(out byte flags, out int offset, out int length, out ReadOnlySequence<byte> buffer)
        {
            flags = Flags;
            offset = Offset;
            length = Length;
            buffer = Buffer;
        }
    }
}