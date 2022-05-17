namespace System.Net.Mqtt.Client.Exceptions;

public class MqttNotAuthorizedException : MqttConnectionException
{
    public MqttNotAuthorizedException() :
        base("Connection refused. Not authorized.")
    { }

    public MqttNotAuthorizedException(string message) : base(message) { }

    public MqttNotAuthorizedException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public new static void Throw() => throw new MqttNotAuthorizedException();
}