namespace System.Net.Mqtt.Client.Exceptions;

public class MqttInvalidProtocolVersionException : MqttConnectionException
{
    public MqttInvalidProtocolVersionException() :
        base("Connection refused. Protocol version was not accepted.")
    { }

    public MqttInvalidProtocolVersionException(string message) : base(message) { }

    public MqttInvalidProtocolVersionException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public new static void Throw() => throw new MqttInvalidProtocolVersionException();
}