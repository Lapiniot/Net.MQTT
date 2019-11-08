using System.Net.Mqtt.Client.Properties;

namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttInvalidProtocolVersionException : MqttConnectionException
    {
        public MqttInvalidProtocolVersionException() : base(Strings.ProtocolVersionNotExcepted) {}

        public MqttInvalidProtocolVersionException(string message) : base(message) {}

        public MqttInvalidProtocolVersionException(string message, Exception innerException) : base(message, innerException) {}
    }
}