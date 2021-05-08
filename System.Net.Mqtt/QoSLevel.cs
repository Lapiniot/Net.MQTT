using System.Diagnostics.CodeAnalysis;

namespace System.Net.Mqtt
{
    [SuppressMessage("Microsoft.Design", "CA1027: Mark enums with FlagsAttribute")]
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