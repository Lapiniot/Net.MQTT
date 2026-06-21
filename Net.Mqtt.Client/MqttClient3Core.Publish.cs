using static System.Threading.Tasks.TaskCreationOptions;
using static Net.Mqtt.PacketFlags;

namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    public override async Task PublishAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        var qos = (byte)qosLevel;
        var flags = (byte)(retain ? Retain : 0);

        var completionSource = new TaskCompletionSource(RunContinuationsAsynchronously);
        ushort id = 0;

        try
        {
            if (qos is 0)
            {
                PostPublish(flags, 0, topic, payload, completionSource);
            }
            else
            {
                flags |= (byte)(qos << 1);
                await inflightSentinel.WaitAsync(cancellationToken).ConfigureAwait(false);
                id = sessionState!.CreateMessageDeliveryState(new PublishDeliveryState(flags, topic, payload));
                OnMessageDeliveryStarted();

                PostPublish(flags, id, topic, payload, completionSource);
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
            PostPublish((byte)(state.Flags | Duplicate), id, state.Topic, state.Payload);
        else
            Post(PubRelPacketMask | id);

        OnMessageDeliveryStarted();
    }
}