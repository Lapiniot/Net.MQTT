namespace Net.Mqtt.Packets.V5;

public enum ReasonCode
{
    Success,
    NoMatchingSubscribers = 0x10,
    UnspecifiedError = 0x80,
    ImplementationSpecificError = 0x83,
    NotAuthorized = 0x87,
    TopicNameInvalid = 0x90,
    PacketIdentifierInUse = 0x91,
    PacketIdentifierNotFound = 0x92,
    QuotaExceeded = 0x97,
    PayloadFormatInvalid = 0x99
}