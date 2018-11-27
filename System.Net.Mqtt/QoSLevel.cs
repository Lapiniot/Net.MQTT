namespace System.Net.Mqtt
{
    public enum QoSLevel
    {
        AtMostOnce = 0,
        AtLeastOnce = 1,
        ExactlyOnce = 2,
        QoS0 = AtMostOnce,
        QoS1 = AtLeastOnce,
        QoS2 = ExactlyOnce
    }
}