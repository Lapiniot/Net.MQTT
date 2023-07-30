namespace System.Net.Mqtt.Packets.V5;

public sealed class PubAckPacket : PublishResponsePacket, IMqttPacket
{
    public PubAckPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : base(id, reasonCode) { }

    public int Write(IBufferWriter<byte> writer, out Span<byte> buffer) =>
        Write(writer, PacketFlags.PubAckPacketMask, Id, ReasonCode, out buffer);
}