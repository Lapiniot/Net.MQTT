namespace System.Net.Mqtt.Packets.V5;

public abstract class PublishResponsePacket : MqttPacketWithId
{
    protected PublishResponsePacket(ushort id, ReasonCode reasonCode = 0) : base(id) => ReasonCode = reasonCode;

    public ReasonCode ReasonCode { get; set; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> reminder, out ushort id, out ReasonCode reasonCode)
    {
        id = 0;
        reasonCode = 0;

        if (reminder.IsSingleSegment)
        {
            if (reminder.FirstSpan is { Length: >= 2 and var len } span)
            {
                id = BinaryPrimitives.ReadUInt16BigEndian(span);
                if (len > 2)
                    reasonCode = (ReasonCode)span[2];
                return true;
            }
        }
        else
        {
            var reader = new SequenceReader<byte>(reminder);
            if (reader.TryReadBigEndian(out short value))
            {
                id = (ushort)value;
                reader.TryRead(out var rc);
                reasonCode = (ReasonCode)rc;
                return true;
            }
        }

        return false;
    }

    protected static int Write([NotNull] IBufferWriter<byte> writer, uint mask, ushort id, ReasonCode reasonCode)
    {
        if (reasonCode is ReasonCode.Success)
        {
            var span4 = writer.GetSpan(4);
            BinaryPrimitives.WriteUInt32BigEndian(span4, mask | 0b00000000_00000010_00000000_00000000u | id);
            writer.Advance(4);
            return 4;
        }

        var span = writer.GetSpan(5);
        span[4] = (byte)reasonCode;
        BinaryPrimitives.WriteUInt32BigEndian(span, mask | 0b00000000_00000011_00000000_00000000u | id);
        writer.Advance(5);
        return 5;
    }
}