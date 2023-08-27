namespace System.Net.Mqtt.Exceptions;

public sealed class PacketTooLargeException : MqttException
{
    public PacketTooLargeException() : base(S.PacketTooLarge) { }

    public PacketTooLargeException(string message) : base(message) { }

    public PacketTooLargeException(string message, Exception innerException) : base(message, innerException) { }

    [DoesNotReturn]
    public static void Throw() => throw new PacketTooLargeException();
}