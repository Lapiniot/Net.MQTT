namespace System.Net.Mqtt.Server.Exceptions;

public sealed class MissingConnectPacketException : MqttProtocolException
{
    public MissingConnectPacketException() : base(MissingConnectPacket) { }

    public MissingConnectPacketException(string message) : base(message) { }

    public MissingConnectPacketException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public static void Throw() => throw new MissingConnectPacketException();
}