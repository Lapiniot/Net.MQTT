namespace System.Net.Mqtt.Client.Exceptions;

public class MqttNotAuthorizedException : MqttConnectionException
{
    public MqttNotAuthorizedException() : base(S.NotAuthorized) { }

    public MqttNotAuthorizedException(string message) : base(message) { }

    public MqttNotAuthorizedException(string message, Exception innerException) : base(message, innerException) { }
}