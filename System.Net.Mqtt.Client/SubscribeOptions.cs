namespace System.Net.Mqtt.Client;

public readonly struct SubscribeOptions : IEquatable<SubscribeOptions>
{
    private readonly int flags;

    private SubscribeOptions(int flags) => this.flags = flags;

    public SubscribeOptions(QoSLevel qos, bool noLocal, bool retainAsPublished, RetainHandling retainHandling)
    {
        flags = (int)qos;
        if (noLocal) flags |= 0b0100;
        if (retainAsPublished) flags |= 0b1000;
        flags |= (int)retainHandling;
    }

    public SubscribeOptions(QoSLevel qos, bool noLocal)
    {
        flags = (int)qos;
        if (noLocal) flags |= 0b0100;
    }

    public static SubscribeOptions QoS0 => new(0);
    public static SubscribeOptions QoS1 => new(1);
    public static SubscribeOptions QoS2 => new(2);

    public readonly int Flags => flags;

    public bool Equals(SubscribeOptions other) => flags == other.flags;

    public override bool Equals(object obj) => obj is SubscribeOptions other && flags == other.flags;

    public override int GetHashCode() => flags.GetHashCode();

    public static bool operator ==(SubscribeOptions left, SubscribeOptions right) => left.Equals(right);

    public static bool operator !=(SubscribeOptions left, SubscribeOptions right) => !left.Equals(right);
}

public enum RetainHandling
{
    SendAlways = 0b0000_0000,
    SendIfNew = 0b0001_0000,
    DontSend = 0b0010_0000
}