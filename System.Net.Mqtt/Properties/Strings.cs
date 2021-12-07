namespace System.Net.Mqtt.Properties;

public static class Strings
{
    public const string UnexpectedPacketType = "Unexpected MQTT packet type";
    public const string InvalidDataStream = "Invalid data in the MQTT byte stream";
    public const string RanOutOfIdentifiers = "Ran out of available identifiers";
    public const string MustBeGreaterOrEqualToFormat = "{0} must be greater or equal to {1}";
    public const string MissingPacketId = "Valid packet id must be specified for this QoS level";
    public const string NotEmptyStringExpected = "String must not be null or empty";
    public const string NotEmptyCollectionExpected = "Not empty collection is expected";
    public const string NonZeroPacketIdExpected = "0 is invalid value for packet id";
    public const string InvalidPacketFormat = "Valid {0} packet data was expected";
    public const string InvalidConnAckPacket = "Invalid CONNECT response. Valid CONNACK packet expected";
    public const string CannotAddOutgoingPacket = "Cannot post to the outgoing packets queue";
    public const string MustBePositivePowerOfTwoInRange = "Must be number in the range {{{0} .. {1}}} which is power of two";
    public const string IdIsNotTrackedByPoolFormat = "Seems id={0} is not tracked by this pool. Check your code for consistency";
    public const string ConnectPacketExpected = "CONNECT packet is expected as the first packet in the data pipe";
    public const string ProtocolNameExpected = "Valid MQTT protocol name is expected";
    public const string ProtocolVersionExpected = "MQTT protocol version is expected";
    public const string SchemaNotSupported = "Uri schema is not supported";
    public const string UnsupportedProtocolVersion = "Incompatible MQTT protocol version";
}