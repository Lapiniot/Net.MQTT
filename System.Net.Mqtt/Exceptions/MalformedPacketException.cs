namespace System.Net.Mqtt.Exceptions;

public sealed class MalformedPacketException : MqttException
{
    public MalformedPacketException() : this(S.MalformedPacket)
    {
    }

    public MalformedPacketException(string message) : base(message)
    {
    }

    public MalformedPacketException(string message, Exception innerException) : base(message, innerException)
    {
    }

    [DoesNotReturn]
    public static void Throw(string packetType) => throw new MalformedPacketException($"Valid '{packetType}' packet data was expected.");

    [DoesNotReturn]
    public static void Throw() => throw new MalformedPacketException();
}