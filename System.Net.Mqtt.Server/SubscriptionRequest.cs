namespace System.Net.Mqtt.Server;

public readonly struct SubscriptionRequest : IEquatable<SubscriptionRequest>
{
    public MqttServerSessionState State { get; }
    public IEnumerable<(string topic, byte qosLevel)> Filters { get; }

    public SubscriptionRequest(MqttServerSessionState state, IEnumerable<(string topic, byte qosLevel)> filters)
    {
        State = state;
        Filters = filters;
    }

    public override bool Equals(object obj)
    {
        return obj is SubscriptionRequest other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(State, Filters);
    }

    public void Deconstruct(out MqttServerSessionState state, out IEnumerable<(string topic, byte qosLevel)> array)
    {
        state = State;
        array = Filters;
    }

    public bool Equals(SubscriptionRequest other)
    {
        return EqualityComparer<MqttServerSessionState>.Default.Equals(State, other.State) && other.Filters?.SequenceEqual(Filters) == true;
    }

    public static bool operator ==(SubscriptionRequest left, SubscriptionRequest right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SubscriptionRequest left, SubscriptionRequest right)
    {
        return !left.Equals(right);
    }
}