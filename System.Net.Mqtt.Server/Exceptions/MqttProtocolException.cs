namespace System.Net.Mqtt.Server.Exceptions;

public abstract class MqttProtocolException : Exception
{
    protected MqttProtocolException()
    {
    }

    protected MqttProtocolException(string message) : base(message)
    {
    }

    protected MqttProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}