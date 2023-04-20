namespace System.Net.Mqtt;

public enum PacketType
{
    None = 0b0000,
    Connect = 0b0001,
    ConnAck = 0b0010,
    Publish = 0b0011,
    PubAck = 0b0100,
    PubRec = 0b0101,
    PubRel = 0b0110,
    PubComp = 0b0111,
    Subscribe = 0b1000,
    SubAck = 0b1001,
    Unsubscribe = 0b1010,
    UnsubAck = 0b1011,
    PingReq = 0b1100,
    PingResp = 0b1101,
    Disconnect = 0b1110,
    Auth = 0b1111
}