namespace Net.Mqtt.Exceptions;

public sealed class ReceiveMaximumExceededException : MqttException
{
    public ReceiveMaximumExceededException() : base(S.ReceiveMaximumExceeded) { }

    public ReceiveMaximumExceededException(string message) : base(message) { }

    public ReceiveMaximumExceededException(string message, Exception innerException) :
        base(message, innerException)
    { }

    [DoesNotReturn]
    public static void Throw(ushort receiveMaximum) =>
        throw new ReceiveMaximumExceededException($"ReceiveMaximum ({receiveMaximum}) exceeded.");
}