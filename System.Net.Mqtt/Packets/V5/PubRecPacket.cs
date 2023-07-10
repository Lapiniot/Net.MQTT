namespace System.Net.Mqtt.Packets.V5;

public sealed class PubRecPacket : PublishResponsePacket
{
    public PubRecPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : base(id, reasonCode) { }

    public override int Write(IBufferWriter<byte> writer, out Span<byte> buffer) =>
        Write(writer, PacketFlags.PubRecPacketMask, Id, ReasonCode, out buffer);
}