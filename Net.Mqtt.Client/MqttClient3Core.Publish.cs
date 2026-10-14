using static System.Threading.Tasks.TaskCreationOptions;
using static Net.Mqtt.PacketFlags;

namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    public override async Task PublishAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        uint qos = (byte)qosLevel;
        var flags = retain ? Retain : 0u;

        var completionSource = new TaskCompletionSource(RunContinuationsAsynchronously);
        ushort id = 0;

        try
        {
            if (qos is 0)
            {
                Post((topic, payload, 0), completionSource);
            }
            else
            {
                flags |= qos << 1;
                await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
                id = sessionState!.CreateMessageDeliveryState(new PublishDeliveryState(flags, topic, payload));
                OnMessageDeliveryStarted();

                Post((topic, payload, ((uint)id << 8) | flags), completionSource);
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

    private void ResendPublish(ushort id, ref readonly PublishDeliveryState state)
    {
        if (!state.Topic.IsEmpty)
        {
            Post((state.Topic, state.Payload, ((uint)id << 8) | state.Flags | Duplicate));
        }
        else
        {
            Post(PubRelPacketMask | id);
        }

        OnMessageDeliveryStarted();
    }
}