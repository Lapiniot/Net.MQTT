namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttInvalidUserCredentialsException : MqttConnectException
    {
        public MqttInvalidUserCredentialsException() :
            base("Connection refused. Bad user name or password.") {}
    }
}