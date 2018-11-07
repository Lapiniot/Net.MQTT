using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents custom MQTT packet data handler delegate
    /// </summary>
    /// <param name="header">MQTT fixed header byte #1</param>
    /// <param name="remainder">Remaining variable packet data</param>
    /// <param name="token"><seealso cref="CancellationToken" /> to support cancellation</param>
    /// <returns><seealso cref="Task" /> that can be awaited</returns>
    public delegate Task MqttPacketHandler(byte header, ReadOnlySequence<byte> remainder, CancellationToken token);
}