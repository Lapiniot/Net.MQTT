using System.Net.Mqtt.Client.Properties;

namespace System.Net.Mqtt.Client.Exceptions;

public class MqttNotAuthorizedException : MqttConnectionException
{
    public MqttNotAuthorizedException() : base(Strings.NotAuthorized) { }

    public MqttNotAuthorizedException(string message) : base(message) { }

    public MqttNotAuthorizedException(string message, Exception innerException) : base(message, innerException) { }
}