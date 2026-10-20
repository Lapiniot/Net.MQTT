namespace Net.Mqtt.Packets.V5;

public sealed class PubAckPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : PublishResponsePacket(id, reasonCode), IMqttPacketWriter5
{
    public PacketWriteResult Write(IBufferWriter<byte> writer, int maxAllowedBytes) => (Write(writer, PacketFlags.PubAckPacketMask, Id, ReasonCode), PacketType.PUBACK);
}