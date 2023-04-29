namespace System.Net.Mqtt;

public abstract class MqttPacketWithId : MqttPacket
{
    protected MqttPacketWithId(ushort id)
    {
        if (id == 0)
            Verify.ThrowValueMustBeInRange(nameof(id), 1, ushort.MaxValue);
        Id = id;
    }

    public ushort Id { get; }
}