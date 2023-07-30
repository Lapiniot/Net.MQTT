namespace System.Net.Mqtt.Packets.V5;

public sealed class PubRelPacket : PublishResponsePacket, IMqttPacket5
{
    public PubRelPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : base(id, reasonCode) { }

    public int Write(IBufferWriter<byte> writer, int maxAllowedBytes, out Span<byte> buffer) =>
        Write(writer, PacketFlags.PubRelPacketMask, Id, ReasonCode, out buffer);
}