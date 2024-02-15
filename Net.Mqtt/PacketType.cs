namespace Net.Mqtt;

public enum PacketType
{
    NONE = 0b0000,
    CONNECT = 0b0001,
    CONNACK = 0b0010,
    PUBLISH = 0b0011,
    PUBACK = 0b0100,
    PUBREC = 0b0101,
    PUBREL = 0b0110,
    PUBCOMP = 0b0111,
    SUBSCRIBE = 0b1000,
    SUBACK = 0b1001,
    UNSUBSCRIBE = 0b1010,
    UNSUBACK = 0b1011,
    PINGREQ = 0b1100,
    PINGRESP = 0b1101,
    DISCONNECT = 0b1110,
    AUTH = 0b1111
}