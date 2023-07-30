namespace System.Net.Mqtt.Packets.V5;

public sealed class PubCompPacket : PublishResponsePacket, IMqttPacket5
{
    public PubCompPacket(ushort id, ReasonCode reasonCode = ReasonCode.Success) : base(id, reasonCode) { }

    public int Write(IBufferWriter<byte> writer, int maxAllowedBytes, out Span<byte> buffer) =>
        Write(writer, PacketFlags.PubCompPacketMask, Id, ReasonCode, out buffer);
}