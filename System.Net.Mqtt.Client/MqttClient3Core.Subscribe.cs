using System.Collections.Concurrent;
using System.Net.Mqtt.Packets.V3;
using static System.Threading.Tasks.TaskCreationOptions;

namespace System.Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;

    public override async Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
    {
        var acknowledgeTcs = new TaskCompletionSource<object>(RunContinuationsAsynchronously);
        var packetId = sessionState.RentId();
        pendingCompletions.TryAdd(packetId, acknowledgeTcs);

        try
        {
            Post(new SubscribePacket(packetId, topics.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.qos)).ToArray()));
            return await acknowledgeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as byte[];
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out _);
            sessionState.ReturnId(packetId);
        }
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        var acknowledgeTcs = new TaskCompletionSource<object>(RunContinuationsAsynchronously);
        var packetId = sessionState.RentId();
        pendingCompletions.TryAdd(packetId, acknowledgeTcs);

        try
        {
            Post(new UnsubscribePacket(packetId, topics.Select(t => (ReadOnlyMemory<byte>)UTF8.GetBytes(t)).ToArray()));
            await acknowledgeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            pendingCompletions.TryRemove(packetId, out _);
            sessionState.ReturnId(packetId);
        }
    }

    protected void OnSubAck(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SubAckPacket.TryReadPayload(in reminder, (int)reminder.Length, out var packet))
        {
            MalformedPacketException.Throw("SUBACK");
        }

        AcknowledgePacket(packet.Id, packet.Feedback);
    }

    protected void OnUnsubAck(byte header, in ReadOnlySequence<byte> reminder)
    {
        if (!SequenceExtensions.TryReadBigEndian(in reminder, out var id))
        {
            MalformedPacketException.Throw("UNSUBACK");
        }

        AcknowledgePacket(id);
    }

    private void AcknowledgePacket(ushort packetId, object result = null)
    {
        if (pendingCompletions.TryGetValue(packetId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }
}