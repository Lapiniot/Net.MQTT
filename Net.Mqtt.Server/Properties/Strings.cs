﻿namespace Net.Mqtt.Server.Properties;

internal static class Strings
{
    public const string InvalidClientIdentifier = "Invalid client identifier.";
    public const string BadUsernameOrPassword = "Bad username or password.";
    public const string MissingConnectPacket = "CONNECT packet is expected as the first packet in the data pipe.";
    public const string ListenerAlreadyRegistered = "Connection listener with the same name was already registered.";
    public const string CannotConnectBeforeAccept = "Client connection must be accepted before.";
}