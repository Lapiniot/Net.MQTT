namespace System.Net.Mqtt.Packets.V5;

public sealed class PubRelPacket : PublishResponsePacket
{
    public PubRelPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : base(id, reasonCode) { }

    public override int Write(IBufferWriter<byte> writer, out Span<byte> buffer) =>
        Write(writer, PacketFlags.PubRelPacketMask, Id, ReasonCode, out buffer);
}