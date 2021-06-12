using System.Buffers;

namespace System.Net.Mqtt.Packets
{
    public sealed class ConnAckPacket : MqttPacket
    {
        public const byte Accepted = 0x00;
        public const byte ProtocolRejected = 0x01;
        public const byte IdentifierRejected = 0x02;
        public const byte ServerUnavailable = 0x03;
        public const byte CredentialsRejected = 0x04;
        public const byte NotAuthorized = 0x05;

        public ConnAckPacket(byte statusCode, bool sessionPresent = false)
        {
            StatusCode = statusCode;
            SessionPresent = sessionPresent;
        }

        public byte StatusCode { get; set; }

        public bool SessionPresent { get; set; }

        public static bool TryRead(in ReadOnlySequence<byte> sequence, out ConnAckPacket packet)
        {
            packet = null;

            if(sequence.Length < 4) return false;
            if(sequence.IsSingleSegment) return TryRead(sequence.First.Span, out packet);

            var reader = new SequenceReader<byte>(sequence);
            return TryRead(ref reader, out packet);
        }

        public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, out ConnAckPacket packet)
        {
            packet = null;

            if(sequence.Length < 2) return false;
            if(sequence.IsSingleSegment)
            {
                var span = sequence.FirstSpan;
                packet = new ConnAckPacket(span[1], (span[0] & 0x01) == 0x01);
                return true;
            }

            var reader = new SequenceReader<byte>(sequence);
            if(!reader.TryReadBigEndian(out short w)) return false;

            packet = new ConnAckPacket((byte)(w & 0xFF), ((w >> 8) & 0x01) == 0x01);
            return true;
        }

        private static bool TryRead(ref SequenceReader<byte> reader, out ConnAckPacket packet)
        {
            packet = null;

            var remaining = reader.Remaining;

            if(reader.TryReadBigEndian(out short h) && h >> 8 == 0b0010_0000 && (h & 0xFF) == 2 && reader.TryReadBigEndian(out short w))
            {
                packet = new ConnAckPacket((byte)(w & 0xFF), ((w >> 8) & 0x01) == 0x01);
                return true;
            }

            reader.Rewind(remaining - reader.Remaining);
            return false;
        }

        private static bool TryRead(ReadOnlySpan<byte> source, out ConnAckPacket packet)
        {
            if(source[0] != 0b0010_0000 || source[1] != 2)
            {
                packet = null;
                return false;
            }

            packet = new ConnAckPacket(source[3], (source[2] & 0x01) == 0x01);
            return true;
        }

        private static bool TryReadPayload(ReadOnlySpan<byte> source, out ConnAckPacket packet)
        {
            if(source[1] != 2)
            {
                packet = null;
                return false;
            }

            packet = new ConnAckPacket(source[3], (source[2] & 0x01) == 0x01);
            return true;
        }

        #region Overrides of MqttPacket

        public override int GetSize(out int remainingLength)
        {
            remainingLength = 2;
            return 4;
        }

        public override void Write(Span<byte> span, int remainingLength)
        {
            span[0] = 0b0010_0000;
            span[1] = 2;
            span[2] = (byte)(SessionPresent ? 1 : 0);
            span[3] = StatusCode;
        }

        #endregion
    }
}