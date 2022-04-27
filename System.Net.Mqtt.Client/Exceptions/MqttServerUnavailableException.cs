namespace System.Net.Mqtt.Client.Exceptions;

public class MqttServerUnavailableException : MqttConnectionException
{
    public MqttServerUnavailableException() : base(S.ServerUnavailable) { }

    public MqttServerUnavailableException(string message) : base(message) { }

    public MqttServerUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}