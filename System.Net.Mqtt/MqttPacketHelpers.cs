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

        public static async Task<PeekResult> PeekAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            try
            {
                while(true)
                {
                    var task = reader.ReadAsync(cancellationToken);
                    var result = task.IsCompleted ? task.Result : await task.ConfigureAwait(false);
                    var buffer = result.Buffer;

                    try
                    {
                        if(TryParseHeader(buffer, out var flags, out var length, out var offset))
                        {
                            if(buffer.Length < offset + length)
                            {
                                // Not enough data received yet
                                continue;
                            }

                            return new PeekResult(flags, length, offset);
                        }

                        if(buffer.Length >= 5)
                        {
                            // We must stop here, because no valid MQTT packet header
                            // was found within 5 (max possible header size) bytes
                            throw new InvalidDataException(PacketDataExpected);
                        }
                    }
                    finally
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
            }
            finally
            {
                // Notify that we have not consumed any data from the pipe and 
                // cancel current pending Read operation to unblock any further 
                // immediate reads. Otherwise next reader will be blocked until 
                // new portion of data is read from network socket and flushed out
                // by writer task. Essentially, this is just a simulation of "Peek"
                // operation in terms of pipelines API.
                reader.CancelPendingRead();
            }
        }
    }

    public struct PeekResult
    {
        internal PeekResult(byte flags, int length, int offset)
        {
            Flags = flags;
            Length = length;
            PayloadOffset = offset;
        }

        public byte Flags { get; }
        public int Length { get; }
        public int PayloadOffset { get; }

        public void Deconstruct(out byte flags, out int length, out int payloadOffset)
        {
            flags = Flags;
            length = Length;
            payloadOffset = PayloadOffset;
        }
    }
}