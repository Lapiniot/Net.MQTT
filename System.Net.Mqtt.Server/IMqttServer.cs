namespace System.Net.Mqtt.Server
{
    public interface IMqttServer
    {
        void OnMessage(Message message, string clientId);
        void OnSubscribe(MqttServerSessionState state, (string filter, byte qosLevel)[] filters);
    }
}