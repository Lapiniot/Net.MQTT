using System.Policies;

namespace System.Net.Mqtt.Client;

public sealed class MqttClient4 : MqttClient
{
    public MqttClient4(NetworkConnection connection, string clientId, ClientSessionStateRepository repository,
        IRetryPolicy reconnectPolicy, bool disposeTransport) :
        base(connection, clientId, repository, reconnectPolicy, disposeTransport)
    { }

    public override byte ProtocolLevel => 0x04;

    public override string ProtocolName => "MQTT";

    public async Task ConnectAsync(MqttConnectionOptions options, bool waitForAcknowledgement, CancellationToken cancellationToken = default)
    {
        await ConnectAsync(options, cancellationToken).ConfigureAwait(false);

        if (waitForAcknowledgement)
        {
            await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
    {
        if (!ConnectionAcknowledged)
        {
            await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
        }

        return await base.SubscribeAsync(topics, cancellationToken).ConfigureAwait(false);
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        if (!ConnectionAcknowledged)
        {
            await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
        }

        await base.UnsubscribeAsync(topics, cancellationToken).ConfigureAwait(false);
    }

    public override async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
    {
        if (qosLevel != QoSLevel.QoS0 && !ConnectionAcknowledged)
        {
            await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
        }

        await base.PublishAsync(topic, payload, qosLevel, retain, cancellationToken).ConfigureAwait(false);
    }
}