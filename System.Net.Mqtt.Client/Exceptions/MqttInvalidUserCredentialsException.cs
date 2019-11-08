using System.Net.Mqtt.Client.Properties;

namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttInvalidUserCredentialsException : MqttConnectionException
    {
        public MqttInvalidUserCredentialsException() : base(Strings.BadUserNameOrPassword) {}

        public MqttInvalidUserCredentialsException(string message) : base(message) {}

        public MqttInvalidUserCredentialsException(string message, Exception innerException) : base(message, innerException) {}
    }
}