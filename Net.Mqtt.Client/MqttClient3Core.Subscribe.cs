using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    public override async Task<ReadOnlyMemory<byte>> SubscribeAsync((string topic, QoSLevel qos)[] filters, CancellationToken cancellationToken = default)
    {
        var packetId = sessionState!.RentId();

        try
        {
            using var ack = AcquirePacketAcknowledgementCookie(packetId);
            Post(new SubscribePacket(packetId, [.. filters.Select(t => ((ReadOnlyMemory<byte>)UTF8.GetBytes(t.topic), (byte)t.qos))]));
            return await ack.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sessionState.ReturnId(packetId);
        }
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        var packetId = sessionState!.RentId();

        try
        {
            using var ack = AcquirePacketAcknowledgementCookie(packetId);
            Post(new UnsubscribePacket(packetId, [.. topics.Select(t => (ReadOnlyMemory<byte>)UTF8.GetBytes(t))]));
            await ack.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sessionState.ReturnId(packetId);
        }
    }
}