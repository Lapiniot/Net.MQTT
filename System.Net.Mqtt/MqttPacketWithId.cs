namespace System.Net.Mqtt;

public abstract class MqttPacketWithId : MqttPacket
{
    protected MqttPacketWithId(ushort id)
    {
        if (id == 0)
            Verify.ThrowValueMustBeInRange(nameof(id), 1, ushort.MaxValue);
        Id = id;
    }

    public ushort Id { get; }

    protected abstract byte Header { get; }

    #region Overrides of MqttPacket

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = Header;
        span[1] = 2;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2), Id);
    }

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 2;
        return 4;
    }

    #endregion
}