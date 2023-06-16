namespace System.Net.Mqtt.Server.Exceptions;

public sealed class InvalidCredentialsException : MqttException
{
    public InvalidCredentialsException() : base(BadUsernameOrPassword) { }

    public InvalidCredentialsException(string message) : base(message) { }

    public InvalidCredentialsException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public static void Throw() => throw new InvalidCredentialsException();
}