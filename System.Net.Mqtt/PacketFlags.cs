namespace System.Net.Mqtt
{
    public static class PacketFlags
    {
        public const byte Duplicate = 0b1000;
        public const byte QoSLevel0 = 0b0000;
        public const byte QoSLevel1 = 0b0010;
        public const byte QoSLevel2 = 0b0100;
        public const byte Retain = 0b0001;
        public const byte TypeMask = 0b1111_0000;
        public const byte QoSMask = 0b0000_0110;
    }
}