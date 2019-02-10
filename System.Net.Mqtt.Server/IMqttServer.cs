namespace System.Net.Mqtt.Server
{
    public delegate T CreateSessionStateFactory<out T>(string clientId) where T : SessionState;

    public interface IMqttServer
    {
        void OnMessage(Message message);
        void OnSubscribe(SessionState state, (string filter, byte qosLevel)[] filters);
        T GetOrCreateState<T>(string clientId, bool clean, CreateSessionStateFactory<T> createFactory) where T : SessionState;
        void RemoveSessionState(string clientId);
    }
}