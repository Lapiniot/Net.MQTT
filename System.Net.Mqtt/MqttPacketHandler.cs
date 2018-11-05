using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents custom MQTT packet handler delegate
    /// </summary>
    /// <param name="buffer">Source binary data to parse</param>
    /// <param name="token"><seealso cref="CancellationToken" /> to support cancellation</param>
    /// <returns>
    /// <seealso cref="ValueTask{TResult}" /> that can be awaited and contains actual
    /// amount of data successfully parsed from the source, otherwise must return 0 (!)
    /// </returns>
    public delegate ValueTask<int> MqttPacketHandler(ReadOnlySequence<byte> buffer, CancellationToken token);
}