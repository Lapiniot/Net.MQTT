using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public sealed partial class MqttClient5
{
    public override async Task PublishAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.QoS0, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ushort id = 0;

        try
        {
            if (qosLevel is QoSLevel.QoS0)
            {
                Post(new PublishPacket(0, qosLevel, topic, payload, retain), completionSource);
            }
            else
            {
                if (!ConnectionAcknowledged)
                {
                    await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);
                }

                await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
                id = sessionState!.CreateMessageDeliveryState(new(topic, payload, qosLevel, retain));
                OnMessageDeliveryStarted();

                Post(new PublishPacket(id, qosLevel, topic, payload, retain), completionSource);
            }

            await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (id is not 0)
            {
                CompleteMessageDelivery(id);
            }

            throw;
        }
    }

    public async Task PublishAsync(Message message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ushort id = 0;

        try
        {
            if (message.QoSLevel is QoSLevel.QoS0)
            {
                Post(CreatePacket(message, 0), completionSource);
            }
            else
            {
                if (!ConnectionAcknowledged)
                {
                    await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);
                }

                await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
                id = sessionState!.CreateMessageDeliveryState(message);
                OnMessageDeliveryStarted();

                Post(CreatePacket(message, id), completionSource);
            }

            await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (id is not 0)
            {
                CompleteMessageDelivery(id);
            }

            throw;
        }
    }

    private static PublishPacket CreatePacket(Message message, ushort id)
    {
        return new PublishPacket(id, message.QoSLevel, message.Topic, message.Payload, message.Retain)
        {
            ContentType = message.ContentType,
            CorrelationData = message.CorrelationData,
            MessageExpiryInterval = message.ExpiryInterval,
            PayloadFormat = message.PayloadFormat,
            ResponseTopic = message.ResponseTopic,
            UserProperties = message.UserProperties
        };
    }

    private void ResendPublish(ushort id, Message? message)
    {
        if (message is not null)
        {
            Post(new PublishPacket(id, message.QoSLevel, message.Topic, message.Payload, message.Retain, duplicate: true)
            {
                MessageExpiryInterval = message.ExpiryInterval,
                ContentType = message.ContentType,
                PayloadFormat = message.PayloadFormat,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                UserProperties = message.UserProperties
            });
        }
        else
        {
            Post(PacketFlags.PubRelPacketMask | id);
        }

        OnMessageDeliveryStarted();
    }
}