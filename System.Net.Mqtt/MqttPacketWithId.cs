using static System.Buffers.Binary.BinaryPrimitives;
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

        #region Overrides of MqttPacket

        public override bool TryWrite(in Memory<byte> buffer, out int size)
        {
            size = 4;
            if(size > buffer.Length) return false;

            var span = buffer.Span;
            span[0] = Header;
            span[1] = 2;
            WriteUInt16BigEndian(span.Slice(2), Id);
            return true;
        }

        #endregion
    }
}