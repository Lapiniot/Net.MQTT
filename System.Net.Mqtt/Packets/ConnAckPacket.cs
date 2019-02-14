using System.Buffers;
using System.Net.Mqtt.Extensions;

namespace System.Net.Mqtt.Packets
{
    public sealed class ConnAckPacket : MqttPacket
    {
        public ConnAckPacket(byte statusCode, bool sessionPresent = false)
        {
            StatusCode = statusCode;
            SessionPresent = sessionPresent;
        }

        public byte StatusCode { get; set; }

        public bool SessionPresent { get; set; }

        #region Overrides of MqttPacket

        public override Memory<byte> GetBytes()
        {
            return new byte[] {(byte)PacketType.ConnAck, 2, (byte)(SessionPresent ? 1 : 0), StatusCode};
        }

        #endregion

        public static bool TryParse(in ReadOnlySequence<byte> sequence, out ConnAckPacket packet)
        {
            if(sequence.IsSingleSegment) return TryParse(sequence.First.Span, out packet);

            if(sequence.Length < 4 ||
               !sequence.TryReadUInt16(out var h) || h >> 8 != 0b0010_0000 || (h & 0xFF) != 2 ||
               !sequence.Slice(2).TryReadUInt16(out var w))
            {
                packet = null;
                return false;
            }

            packet = new ConnAckPacket((byte)(w & 0xFF), ((w >> 8) & 0x01) == 0x01);
            return true;
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out ConnAckPacket packet)
        {
            if(source.Length < 4 || source[0] != 0b0010_0000 || source[1] != 2)
            {
                packet = null;
                return false;
            }

            packet = new ConnAckPacket(source[3], (source[2] & 0x01) == 0x01);
            return true;
        }

        public static class StatusCodes
        {
            public const byte Accepted = 0x00;
            public const byte ProtocolRejected = 0x01;
            public const byte IdentifierRejected = 0x02;
            public const byte ServerUnavailable = 0x03;
            public const byte CredentialsRejected = 0x04;
            public const byte NotAuthorized = 0x05;
        }
    }
}