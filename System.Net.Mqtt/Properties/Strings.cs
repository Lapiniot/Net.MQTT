namespace System.Net.Mqtt.Properties
{
    internal static class Strings
    {
        public const string UnexpectedPacketType = "Unexpected MQTT packet type.";
        public const string InvalidDataStream = "Invalid data in the MQTT byte stream.";
        public const string RanOutOfIdentifiers = "Ran out of available identifiers.";
        public const string MustBeGreaterOrEqualToFormat = "{0} must be greater or equal to {1}";
        public const string MissingPacketId = "Valid packet id must be specified for this QoS level";
        public const string NotEmptyStringExpected = "String must not be null or empty";
        public const string NotEmptyCollectionExpected = "Not empty collection is expected.";
        public const string NonZeroPacketIdExpected = "0 is invalid value for packet id.";
        public const string InvalidPacketTemplate = "Valid {0} packet data was expected.";
    }
}