namespace System.Net.Mqtt.Server
{
    public delegate T CreateSessionStateFactory<out T>(string clientId) where T : MqttServerSessionState;

    public interface IMqttServer
    {
        void OnMessage(Message message, string clientId);
        void OnSubscribe(MqttServerSessionState state, (string filter, byte qosLevel)[] filters);
    }
}