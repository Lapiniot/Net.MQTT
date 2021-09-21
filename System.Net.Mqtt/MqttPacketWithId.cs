using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt;

public abstract class MqttPacketWithId : MqttPacket
{
    protected MqttPacketWithId(ushort id)
    {
        if(id == 0) throw new ArgumentOutOfRangeException(nameof(id), NonZeroPacketIdExpected);
        Id = id;
    }

    public ushort Id { get; }

    protected abstract byte Header { get; }

    #region Overrides of MqttPacket

    public override void Write(Span<byte> span, int remainingLength)
    {
        span[0] = Header;
        span[1] = 2;
        WriteUInt16BigEndian(span[2..], Id);
    }

    public override int GetSize(out int remainingLength)
    {
        remainingLength = 2;
        return 4;
    }

    #endregion
}