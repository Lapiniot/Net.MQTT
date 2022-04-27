using System.Runtime.Serialization;

namespace System.Net.Mqtt.Server.Exceptions;

[Serializable]
public class InvalidClientIdException : Exception
{
    public InvalidClientIdException() : base(InvalidClientIdentifier)
    { }

    public InvalidClientIdException(string message) : base(message)
    { }

    public InvalidClientIdException(string message, Exception innerException) : base(message, innerException)
    { }

    protected InvalidClientIdException(SerializationInfo info, StreamingContext context) : base(info, context)
    { }
}