using System.Runtime.Serialization;

namespace System.Net.Mqtt.Server.Exceptions;

[Serializable]
public class UnsupportedProtocolVersionException : Exception
{
    public UnsupportedProtocolVersionException()
    { }

    public UnsupportedProtocolVersionException(int version) :
        this(UnsupportedProtocolVersion) => Version = version;

    public UnsupportedProtocolVersionException(string message) : base(message)
    { }

    public UnsupportedProtocolVersionException(string message, Exception innerException) : base(message, innerException)
    { }

    protected UnsupportedProtocolVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
    { }

    public int Version { get; }
}