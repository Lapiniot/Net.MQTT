using System.Buffers;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents custom MQTT packet handler delegate
    /// </summary>
    /// <param name="buffer">Source binary data to parse</param>
    /// <param name="consumed">
    /// Should give actual amount of data parsed from the source on success,
    /// otherwise must return 0 (!)
    /// </param>
    /// <returns>
    /// Returns <value>True</value> if handler was able to parse
    /// required data from the source
    /// </returns>
    public delegate bool MqttPacketHandler(in ReadOnlySequence<byte> buffer, out int consumed);
}