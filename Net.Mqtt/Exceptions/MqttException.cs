namespace Net.Mqtt.Exceptions;

public abstract class MqttException : Exception
{
    protected MqttException() { }

    protected MqttException(string message) : base(message) { }

    protected MqttException(string message, Exception innerException) : base(message, innerException) { }
}