namespace System.Net.Mqtt.Client;

public class MqttClientSessionState : MqttSessionState
{
    public MqttClientSessionState(int maxInflight) : base(maxInflight)
    {
    }
}