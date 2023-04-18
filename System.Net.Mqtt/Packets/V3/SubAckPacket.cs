using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets.V3;

public sealed class SubAckPacket : MqttPacketWithId
{
    public SubAckPacket(ushort id, byte[] feedback) : base(id)
    {
        Verify.ThrowIfNullOrEmpty((Array)feedback);

        Feedback = feedback;
    }

    protected override byte Header => SubAckMask;

    public Memory<byte> Feedback { get; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out SubAckPacket packet)
    {
        packet = null;

        var span = sequence.FirstSpan;

        if (length <= span.Length)
        {
            packet = new(BinaryPrimitives.ReadUInt16BigEndian(span), span.Slice(2, length - 2).ToArray());
            return true;
        }
        else if (length <= sequence.Length)
        {
            var reader = new SequenceReader<byte>(sequence);

            if (!reader.TryReadBigEndian(out short id))
                return false;

            var buffer = new byte[length - 2];

            if (!reader.TryCopyTo(buffer))
                return false;

            packet = new((ushort)id, buffer);

            return true;
        }

        return false;
    }

    #region Overrides of MqttPacketWithId

    public override int GetSize(out int remainingLength) => GetSize(Feedback.Length, out remainingLength);

    public override void Write(Span<byte> span, int remainingLength) => Write(span, Id, Feedback.Span, remainingLength);

    public static int GetSize(int feedbackLength, out int remainingLength)
    {
        remainingLength = feedbackLength + 2;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public static void Write(Span<byte> span, ushort packetId, ReadOnlySpan<byte> feedback, int remainingLength)
    {
        span[0] = SubAckMask;
        span = span.Slice(1);
        span = span.Slice(SpanExtensions.WriteMqttVarByteInteger(ref span, remainingLength));
        BinaryPrimitives.WriteUInt16BigEndian(span, packetId);
        span = span.Slice(2);
        feedback.CopyTo(span);
    }

    #endregion
}