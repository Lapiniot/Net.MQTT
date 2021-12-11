namespace System.Net.Mqtt;

public enum QoSLevel
{
    QoS0 = 0,
    QoS1 = 1,
    QoS2 = 2,
    AtMostOnce = QoS0,
    AtLeastOnce = QoS1,
    ExactlyOnce = QoS2
}