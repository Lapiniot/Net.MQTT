namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttInvalidProtocolVersionException : MqttConnectException
    {
        public MqttInvalidProtocolVersionException() : 
            base("Connection refused. Unacceptable protocol version.")
        {
        }
    }
}