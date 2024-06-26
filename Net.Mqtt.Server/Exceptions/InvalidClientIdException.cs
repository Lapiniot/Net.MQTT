namespace Net.Mqtt.Server.Exceptions;

public sealed class InvalidClientIdException : MqttException
{
    public InvalidClientIdException() : base(InvalidClientIdentifier) { }

    public InvalidClientIdException(string message) : base(message) { }

    public InvalidClientIdException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public static void Throw() => throw new InvalidClientIdException();
}