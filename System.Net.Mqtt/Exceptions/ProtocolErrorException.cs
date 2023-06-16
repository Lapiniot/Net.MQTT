namespace System.Net.Mqtt.Exceptions;

public sealed class ProtocolErrorException : MqttException
{
    public ProtocolErrorException() : this(S.UnexpectedPacket)
    {
    }

    public ProtocolErrorException(string message) : base(message)
    {
    }

    public ProtocolErrorException(string message, Exception innerException) : base(message, innerException)
    {
    }

    [DoesNotReturn]
    public static void Throw(byte type) =>
        throw new ProtocolErrorException($"Unexpected '0x{type:x2}' MQTT packet type.");

    [DoesNotReturn]
    public static void Throw() => throw new ProtocolErrorException();
}