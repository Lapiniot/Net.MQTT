namespace System.Net.Mqtt.Packets.V5;

public sealed class PubCompPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : PublishResponsePacket(id, reasonCode), IMqttPacket5
{
    public int Write(IBufferWriter<byte> writer, int maxAllowedBytes) => Write(writer, PacketFlags.PubCompPacketMask, Id, ReasonCode);
}