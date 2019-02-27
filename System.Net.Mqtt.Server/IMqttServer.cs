namespace System.Net.Mqtt.Server
{
    public delegate T CreateSessionStateFactory<out T>(string clientId) where T : SessionState;

    public interface IMqttServer
    {
        void OnMessage(Message message, string clientId);
        void OnSubscribe(SessionState state, (string filter, byte qosLevel)[] filters);
    }
}