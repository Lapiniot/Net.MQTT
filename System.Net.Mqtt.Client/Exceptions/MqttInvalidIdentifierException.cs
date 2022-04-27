namespace System.Net.Mqtt.Client.Exceptions;

public class MqttInvalidIdentifierException : MqttConnectionException
{
    public MqttInvalidIdentifierException() : base(S.IdentifierRejected) { }

    public MqttInvalidIdentifierException(string message) : base(message) { }

    public MqttInvalidIdentifierException(string message, Exception innerException) : base(message, innerException) { }
}