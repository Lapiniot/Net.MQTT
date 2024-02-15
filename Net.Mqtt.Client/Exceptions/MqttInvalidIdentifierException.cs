namespace Net.Mqtt.Client.Exceptions;

public class MqttInvalidIdentifierException : MqttConnectionException
{
    public MqttInvalidIdentifierException() :
        base("Connection refused. Identifier rejected.")
    { }

    public MqttInvalidIdentifierException(string message) : base(message) { }

    public MqttInvalidIdentifierException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public static new void Throw() => throw new MqttInvalidIdentifierException();
}