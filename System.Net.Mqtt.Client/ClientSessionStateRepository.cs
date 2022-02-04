namespace System.Net.Mqtt.Client;

public abstract class ClientSessionStateRepository : ISessionStateRepository<MqttClientSessionState>
{
    public abstract MqttClientSessionState GetOrCreate(string clientId, bool clean, out bool existed);
    public abstract void Remove(string clientId);
}

internal sealed class DefaultClientSessionStateRepository : ClientSessionStateRepository
{
    private MqttClientSessionState sessionState;

    public override MqttClientSessionState GetOrCreate(string clientId, bool cleanSession, out bool existingSession)
    {
        if(cleanSession) Remove(clientId);
        existingSession = sessionState != null;
        return sessionState ?? new MqttClientSessionState();
    }

    public override void Remove(string clientId)
    {
        sessionState = null;
    }
}