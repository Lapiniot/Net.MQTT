namespace System.Net.Mqtt.Packets.V5;

public sealed class PubRelPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : PublishResponsePacket(id, reasonCode), IMqttPacket5
{
    public int Write(IBufferWriter<byte> writer, int maxAllowedBytes, out Span<byte> buffer) =>
        Write(writer, PacketFlags.PubRelPacketMask, Id, ReasonCode, out buffer);
}