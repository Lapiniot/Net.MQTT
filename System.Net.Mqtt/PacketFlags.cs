namespace System.Net.Mqtt;

public static class PacketFlags
{
    public const byte Duplicate = 0b1000;
    public const byte QoSLevel0 = 0b0000;
    public const byte QoSLevel1 = 0b0010;
    public const byte QoSLevel2 = 0b0100;
    public const byte Retain = 0b0001;
    public const byte TypeMask = 0b1111_0000;
    public const byte QoSMask = 0b0000_0011;
    public const byte CleanSessionMask = 0b0000_0010;
    public const byte CleanStartMask = 0b0000_0010;
    public const byte WillMask = 0b0000_0100;
    public const byte WillRetainMask = 0b0010_0000;
    public const byte PasswordMask = 0b0100_0000;
    public const byte UserNameMask = 0b1000_0000;

    public const byte ConnectMask = 0b0001_0000;
    public const byte ConnAckMask = 0b0010_0000;
    public const byte PublishMask = 0b0011_0000;
    public const byte PubAckMask = 0b0100_0000;
    public const byte PubRecMask = 0b0101_0000;
    public const byte PubRelMask = 0b0110_0010;
    public const byte PubCompMask = 0b0111_0000;
    public const byte SubscribeMask = 0b1000_0010;
    public const byte SubAckMask = 0b1001_0000;
    public const byte UnsubscribeMask = 0b1010_0010;
    public const byte UnsubAckMask = 0b1011_0000;
    public const byte PingMask = 0b1100_0000;
    public const byte PingRespMask = 0b1101_0000;
    public const byte DisconnectMask = 0b1110_0000;

    public const uint PubAckPacketMask = 0b01000000_00000010_00000000_00000000u;
    public const uint PubRecPacketMask = 0b01010000_00000010_00000000_00000000u;
    public const uint PubRelPacketMask = 0b01100010_00000010_00000000_00000000u;
    public const uint PubCompPacketMask = 0b01110000_00000010_00000000_00000000u;
    public const uint UnsubAckPacketMask = 0b10110000_00000010_00000000_00000000u;
    public const uint PingReqPacket = 0b11000000_00000000u;
    public const ushort PingRespPacket = (ushort)0b11010000_00000000u;
    public const ushort DisconnectPacket16 = (ushort)0b11100000_00000000u;
    public const uint DisconnectPacket32 = 0b11100000_00000000_00000000_00000000u;
}