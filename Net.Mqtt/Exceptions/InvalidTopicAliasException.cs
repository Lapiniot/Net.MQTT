namespace Net.Mqtt.Exceptions;

public class InvalidTopicAliasException : MqttException
{
    public InvalidTopicAliasException() : this(S.InvalidTopicAlias)
    {
    }

    public InvalidTopicAliasException(string message) : base(message)
    {
    }

    public InvalidTopicAliasException(string message, Exception innerException) : base(message, innerException)
    {
    }

    [DoesNotReturn]
    public static void Throw() => throw new InvalidTopicAliasException();
}