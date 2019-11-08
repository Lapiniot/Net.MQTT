using System.Net.Mqtt.Client.Properties;

namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttServerUnavailableException : MqttConnectionException
    {
        public MqttServerUnavailableException() : base(Strings.ServerUnavailable) {}

        public MqttServerUnavailableException(string message) : base(message) {}

        public MqttServerUnavailableException(string message, Exception innerException) : base(message, innerException) {}
    }
}