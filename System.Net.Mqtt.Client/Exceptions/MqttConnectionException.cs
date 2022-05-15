namespace System.Net.Mqtt.Client.Exceptions;

public class MqttConnectionException : Exception
{
    public MqttConnectionException(string message) : base(message) { }

    public MqttConnectionException() { }

    public MqttConnectionException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public static void Throw() => throw new MqttConnectionException();
}