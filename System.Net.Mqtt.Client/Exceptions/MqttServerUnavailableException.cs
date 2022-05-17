namespace System.Net.Mqtt.Client.Exceptions;

public class MqttServerUnavailableException : MqttConnectionException
{
    public MqttServerUnavailableException() :
        base("Connection refused. Server unavailable.")
    { }

    public MqttServerUnavailableException(string message) : base(message) { }

    public MqttServerUnavailableException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public static new void Throw() => throw new MqttServerUnavailableException();
}