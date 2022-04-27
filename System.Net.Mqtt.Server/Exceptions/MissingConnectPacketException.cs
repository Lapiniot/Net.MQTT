using System.Runtime.Serialization;

namespace System.Net.Mqtt.Server.Exceptions;

[Serializable]
public class MissingConnectPacketException : Exception
{
    public MissingConnectPacketException() : base(ConnectPacketExpected)
    { }

    public MissingConnectPacketException(string message) : base(message)
    { }

    public MissingConnectPacketException(string message, Exception innerException) : base(message, innerException)
    { }

    protected MissingConnectPacketException(SerializationInfo info, StreamingContext context) : base(info, context)
    { }
}