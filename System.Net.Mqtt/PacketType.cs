namespace System.Net.Mqtt
{
    public enum PacketType
    {
        Reserved = 0b0000_0000,
        Connect = 0b0001_0000,
        ConnAck = 0b0010_0000,
        Publish = 0b0011_0000,
        PubAck = 0b0100_0000,
        PubRec = 0b0101_0000,
        PubRel = 0b0110_0000,
        PubComp = 0b0111_0000,
        Subscribe = 0b1000_0000,
        SubAck = 0b1001_0000,
        Unsubscribe = 0b1010_0000,
        UnsubAck = 0b1011_0000,
        PingReq = 0b1100_0000,
        PingResp = 0b1101_0000,
        Disconnect = 0b1110_0000
    }
}