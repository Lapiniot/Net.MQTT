namespace System.Net.Mqtt.Client;

public abstract class ClientSessionStateRepository : ISessionStateRepository<MqttClientSessionState>
{
    public abstract MqttClientSessionState GetOrCreate(string clientId, bool clean, out bool existed);
    public abstract void Remove(string clientId);
}

internal sealed class DefaultClientSessionStateRepository : ClientSessionStateRepository
{
    private readonly int maxInFlight;

    public DefaultClientSessionStateRepository(int maxInFlight)
    {
        Verify.ThrowIfNotInRange(maxInFlight, 1, ushort.MaxValue);
        this.maxInFlight = maxInFlight;
    }

    private MqttClientSessionState sessionState;

    public override MqttClientSessionState GetOrCreate(string clientId, bool clean, out bool existed)
    {
        if (clean) Remove(clientId);
        existed = sessionState != null;
        return sessionState ?? new MqttClientSessionState(maxInFlight);
    }

    public override void Remove(string clientId) => sessionState = null;
}