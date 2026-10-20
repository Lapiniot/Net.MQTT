namespace Net.Mqtt.Packets.V5;

public sealed class PubRecPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : PublishResponsePacket(id, reasonCode), IMqttPacketWriter5
{
    public PacketWriteResult Write(IBufferWriter<byte> writer, int maxAllowedBytes) => (Write(writer, PacketFlags.PubRecPacketMask, Id, ReasonCode), PacketType.PUBREC);
}