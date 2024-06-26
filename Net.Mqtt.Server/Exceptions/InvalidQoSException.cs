﻿namespace Net.Mqtt.Server.Exceptions;

public class InvalidQoSException : MqttException
{
    public InvalidQoSException() : this("Invalid QoS level value.")
    {
    }

    public InvalidQoSException(string? message) : base(message)
    {
    }

    public InvalidQoSException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    [DoesNotReturn]
    public static void Throw() => throw new InvalidQoSException();
}