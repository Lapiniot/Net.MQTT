namespace Net.Mqtt.Packets.V5;

public sealed class PubAckPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : PublishResponsePacket(id, reasonCode), IMqttPacket5
{
    public int Write(IBufferWriter<byte> writer, int maxAllowedBytes) => Write(writer, PacketFlags.PubAckPacketMask, Id, ReasonCode);
}