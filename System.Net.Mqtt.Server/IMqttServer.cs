namespace System.Net.Mqtt.Server
{
    public interface IMqttServer
    {
        void OnMessage(Message message);
        void OnSubscribe(SessionState state, (string filter, QoSLevel qosLevel)[] filters);
    }
}