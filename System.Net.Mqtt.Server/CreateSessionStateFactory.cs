namespace System.Net.Mqtt.Server
{
    public delegate T CreateSessionStateFactory<out T>(string clientId) where T : MqttServerSessionState;
}