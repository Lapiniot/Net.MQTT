namespace System.Net.Mqtt;

public abstract class MqttPacketWithId : MqttPacket
{
    protected MqttPacketWithId(ushort id)
    {
        Verify.ThrowIfNotInRange(id, 1, ushort.MaxValue);
        Id = id;
    }

    public ushort Id { get; }
}