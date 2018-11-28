namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttInvalidIdentifierException : MqttConnectException
    {
        public MqttInvalidIdentifierException() :
            base("Connection refused. Identifier rejected.") {}
    }
}