using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets;

public class SubAckPacket : MqttPacketWithId
{
    public SubAckPacket(ushort id, byte[] feedback) : base(id)
    {
        Verify.ThrowIfNullOrEmpty((Array)feedback);

        Feedback = feedback;
    }

    protected override byte Header => SubAckMask;

    public Memory<byte> Feedback { get; }

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out SubAckPacket packet)
    {
        var span = sequence.FirstSpan;
        if (TryReadMqttHeader(in span, out var flags, out var length, out var offset)
            && flags == SubAckMask
            && offset + length <= span.Length)
        {
            var current = span.Slice(offset, length);
            packet = new(ReadUInt16BigEndian(current), current[2..].ToArray());
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if (TryReadMqttHeader(ref reader, out flags, out length)
            && flags == SubAckMask
            && reader.Remaining >= length)
        {
            return TryReadPayload(ref reader, length, out packet);
        }

        reader.Rewind(remaining - reader.Remaining);
        packet = null;
        return false;
    }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, int length, out SubAckPacket packet)
    {
        var span = sequence.FirstSpan;
        if (span.Length >= length)
        {
            packet = new(ReadUInt16BigEndian(span), span[2..length].ToArray());
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        return TryReadPayload(ref reader, length, out packet);
    }

    private static bool TryReadPayload(ref SequenceReader<byte> reader, int length, out SubAckPacket packet)
    {
        packet = null;

        if (!reader.TryReadBigEndian(out short id))
        {
            return false;
        }

        var buffer = new byte[length - 2];

        if (!reader.TryCopyTo(buffer))
        {
            reader.Rewind(2);
            return false;
        }

        packet = new((ushort)id, buffer);

        return true;
    }

    #region Overrides of MqttPacketWithId

    public override int GetSize(out int remainingLength)
    {
        remainingLength = Feedback.Length + 2;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = SubAckMask;
        span = span[1..];
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        WriteUInt16BigEndian(span, Id);
        span = span[2..];
        Feedback.Span.CopyTo(span);
    }

    #endregion
}