using System.Buffers;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents custom MQTT packet data handler delegate
    /// </summary>
    /// <param name="header">MQTT fixed header byte #1</param>
    /// <param name="remainder">Remaining variable length packet data</param>
    public delegate void MqttPacketHandler(byte header, ReadOnlySequence<byte> remainder);
}