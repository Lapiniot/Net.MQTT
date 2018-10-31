using System.Buffers;
using System.Net.Mqtt.Buffers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public abstract class MqttPacketWithId : MqttPacket
    {
        protected MqttPacketWithId(ushort id)
        {
            if(id == 0) throw new ArgumentOutOfRangeException(nameof(id), NonZeroPacketIdExpected);
            Id = id;
        }

        public ushort Id { get; }

        protected abstract byte Header { get; }

        public override Memory<byte> GetBytes()
        {
            return new byte[] {Header, 2, (byte)(Id >> 8), (byte)(Id & 0x00ff)};
        }

        protected internal static bool TryParseGeneric(in ReadOnlySequence<byte> source, PacketType type, out ushort id)
        {
            id = 0;

            if(source.First.Length >= 4)
            {
                return TryParseGeneric(source.First.Span, type, out id);
            }

            var se = new SequenceEnumerator<byte>(source);

            if(!se.MoveNext() || (se.Current & TypeMask) != (byte)type) return false;

            if(!se.MoveNext() || se.Current != 2) return false;

            if(!se.MoveNext()) return false;

            var high = se.Current << 8;

            if(!se.MoveNext()) return false;

            id = (ushort)(high | se.Current);

            return true;
        }

        protected internal static bool TryParseGeneric(in ReadOnlySpan<byte> source, PacketType type, out ushort id)
        {
            if(source.Length < 4 || (source[0] & TypeMask) != (byte)type || source[1] != 2)
            {
                id = 0;
                return false;
            }

            id = (ushort)((source[2] << 8) | source[3]);
            return true;
        }
    }
}