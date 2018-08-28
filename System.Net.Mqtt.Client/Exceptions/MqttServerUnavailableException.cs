namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttServerUnavailableException : MqttConnectException
    {
        public MqttServerUnavailableException() : 
            base("Connection refused. Server unavailable.")
        {
        }
    }
}