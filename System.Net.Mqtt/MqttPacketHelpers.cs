using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Mqtt.Extensions;
using System.Threading;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public static class MqttPacketHelpers
    {
        [Conditional("DEBUG")]
        public static void DebugDump(this MqttPacket packet)
        {
            if(packet == null) throw new ArgumentNullException(nameof(packet));
            var buffer = new byte[packet.GetSize(out var remainingLength)];
            packet.Write(buffer, remainingLength);
            Debug.WriteLine($"{{{string.Join(",", buffer.Select(b => "0x" + b.ToString("x2", InvariantCulture)))}}}");
        }

        public static async ValueTask<PacketReadResult> ReadPacketAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            if(reader == null) throw new ArgumentNullException(nameof(reader));

            while(true)
            {
                var vt = reader.ReadAsync(cancellationToken);
                var result = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);
                var buffer = result.Buffer;

                if(buffer.TryReadMqttHeader(out var flags, out var length, out var offset))
                {
                    if(buffer.Length < offset + length)
                    {
                        // Not enough data received yet
                        continue;
                    }

                    return new PacketReadResult(flags, offset, length, buffer.Slice(0, offset + length));
                }

                if(buffer.Length >= 5)
                {
                    // We must stop here, because no valid MQTT packet header
                    // was found within 5 (max possible header size) bytes
                    throw new InvalidDataException(InvalidDataStream);
                }
            }
        }
    }

    public struct PacketReadResult : IEquatable<PacketReadResult>
    {
        public byte Flags { get; }
        public int Offset { get; }
        public int Length { get; }
        public ReadOnlySequence<byte> Buffer { get; }

        public PacketReadResult(byte flags, int offset, int length, ReadOnlySequence<byte> buffer)
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

        #region Equality members

        public override bool Equals(object obj)
        {
            return obj is PacketReadResult other &&
                   Buffer.Equals(other.Buffer) &&
                   Flags == other.Flags &&
                   Offset == other.Offset &&
                   Length == other.Length;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Flags.GetHashCode();
                hashCode = (hashCode * 397) ^ Offset;
                hashCode = (hashCode * 397) ^ Length;
                hashCode = (hashCode * 397) ^ Buffer.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(PacketReadResult left, PacketReadResult right)
        {
            return left.Buffer.Equals(right.Buffer) &&
                   left.Length == right.Length &&
                   left.Offset == right.Offset &&
                   left.Flags == right.Flags;
        }

        public static bool operator !=(PacketReadResult left, PacketReadResult right)
        {
            return !left.Buffer.Equals(right.Buffer) ||
                   left.Length != right.Length ||
                   left.Offset != right.Offset ||
                   left.Flags != right.Flags;
        }

        #endregion

        #region Implementation of IEquatable<ReadResult>

        public bool Equals(PacketReadResult other)
        {
            return other.Buffer.Equals(Buffer) &&
                   other.Length == Length &&
                   other.Offset == Offset &&
                   other.Flags == Flags;
        }

        #endregion
    }
}