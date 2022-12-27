namespace System.Net.Mqtt.Server.Exceptions;

public sealed class UnsupportedProtocolVersionException : MqttProtocolException
{
    public UnsupportedProtocolVersionException() : base(UnsupportedProtocolVersion) { }

    public UnsupportedProtocolVersionException(int version) : this() => Version = version;

    public UnsupportedProtocolVersionException(string message) : base(message) { }

    public UnsupportedProtocolVersionException(string message, Exception innerException) : base(message, innerException) { }

    public int Version { get; }

    [DoesNotReturn]
    public static void Throw(int version) => throw new UnsupportedProtocolVersionException(version);
}