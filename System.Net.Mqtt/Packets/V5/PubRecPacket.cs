namespace System.Net.Mqtt.Packets.V5;

public sealed class PubRecPacket : PublishResponsePacket, IMqttPacket5
{
    public PubRecPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : base(id, reasonCode) { }

    public int Write(IBufferWriter<byte> writer, int maxAllowedBytes, out Span<byte> buffer) =>
        Write(writer, PacketFlags.PubRecPacketMask, Id, ReasonCode, out buffer);
}