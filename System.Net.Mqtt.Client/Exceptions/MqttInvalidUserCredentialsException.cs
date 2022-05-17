namespace System.Net.Mqtt.Client.Exceptions;

public class MqttInvalidUserCredentialsException : MqttConnectionException
{
    public MqttInvalidUserCredentialsException() :
        base("Connection refused. Bad user name or password.")
    { }

    public MqttInvalidUserCredentialsException(string message) : base(message) { }

    public MqttInvalidUserCredentialsException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public new static void Throw() => throw new MqttInvalidUserCredentialsException();
}