using System.Net.Mqtt.Client.Properties;

namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttInvalidIdentifierException : MqttConnectionException
    {
        public MqttInvalidIdentifierException() : base(Strings.IdentifierRejected) {}

        public MqttInvalidIdentifierException(string message) : base(message) {}

        public MqttInvalidIdentifierException(string message, Exception innerException) : base(message, innerException) {}
    }
}