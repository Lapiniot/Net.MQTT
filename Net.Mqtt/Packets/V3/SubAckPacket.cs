using static Net.Mqtt.PacketFlags;

namespace Net.Mqtt.Packets.V3;

public sealed class SubAckPacket : MqttPacketWithId, IMqttPacket
{
    public SubAckPacket(ushort id, byte[] feedback) : base(id)
    {
        ArgumentNullException.ThrowIfNull(feedback);
        ArgumentOutOfRangeException.ThrowIfZero(feedback.Length);

        Feedback = feedback;
    }

    public ReadOnlyMemory<byte> Feedback { get; }

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

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer)
    {
        var remaining = Feedback.Length + 2;
        var size = 1 + MqttHelpers.GetVarBytesCount((uint)remaining) + remaining;
        var span = writer.GetSpan(size);
        span[0] = SubAckMask;
        span = span.Slice(1);
        SpanExtensions.WriteMqttVarByteInteger(ref span, remaining);
        BinaryPrimitives.WriteUInt16BigEndian(span, Id);
        span = span.Slice(2);
        Feedback.Span.CopyTo(span);
        writer.Advance(size);
        return size;
    }

    #endregion
}