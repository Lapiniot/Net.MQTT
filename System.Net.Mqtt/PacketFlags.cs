namespace System.Net.Mqtt
{
    [Flags]
    public enum PacketFlags
    {
        Duplicate = 0b1000,
        QoSLevel0 = 0b0000,
        QoSLevel1 = 0b0010,
        QoSLevel2 = 0b0100,
        Retain = 0b0001
    }
}