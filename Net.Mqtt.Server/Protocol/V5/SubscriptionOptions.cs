namespace Net.Mqtt.Server.Protocol.V5;

public record struct SubscriptionOptions(byte QoS, byte Flags, uint SubscriptionId)
{
    public readonly bool NoLocal => (Flags & 0b0000_0100) != 0;
    public readonly bool RetainAsPublished => (Flags & 0b0000_1000) != 0;
    public readonly bool RetainSendAlways => (Flags & 0b0011_0000) == 0;
    public readonly bool RetainSendIfNew => (Flags & 0b0001_0000) != 0;
    public readonly bool RetainDoNotSend => (Flags & 0b0010_0000) != 0;
}