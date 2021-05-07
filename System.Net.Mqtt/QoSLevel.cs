namespace System.Net.Mqtt
{
    public enum QoSLevel
    {
        AtMostOnce,
        AtLeastOnce,
        ExactlyOnce,
        QoS0 = AtMostOnce,
        QoS1 = AtLeastOnce,
        QoS2 = ExactlyOnce
    }
}