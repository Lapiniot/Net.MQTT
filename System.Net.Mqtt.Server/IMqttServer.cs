namespace System.Net.Mqtt.Server
{
    public interface IMqttServer
    {
        void OnMessage(Message message);
        void OnSubscribe(SessionState state, (string filter, byte qosLevel)[] filters);
    }
}