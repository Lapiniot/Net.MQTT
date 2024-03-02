namespace Net.Mqtt.Client;

public sealed class MqttClient4(NetworkConnection connection, bool disposeConnection,
    string? clientId, int maxInFlight, IRetryPolicy? reconnectPolicy) :
    MqttClient3Core(connection, disposeConnection, clientId, maxInFlight,
        reconnectPolicy, protocolLevel: 0x04, protocolName: "MQTT")
{
    public async Task ConnectAsync(MqttConnectionOptions3 options, bool waitAcknowledgement = true,
        CancellationToken cancellationToken = default)
    {
        await ConnectCoreAsync(options, cancellationToken).ConfigureAwait(false);

        if (waitAcknowledgement)
            await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] filters,
        CancellationToken cancellationToken = default)
    {
        if (!ConnectionAcknowledged)
            await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);

        return await base.SubscribeAsync(filters, cancellationToken).ConfigureAwait(false);
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        if (!ConnectionAcknowledged)
            await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);

        await base.UnsubscribeAsync(topics, cancellationToken).ConfigureAwait(false);
    }

    public override async Task PublishAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload,
        QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false,
        CancellationToken cancellationToken = default)
    {
        if (qosLevel != QoSLevel.QoS0 && !ConnectionAcknowledged)
            await WaitConnAckReceivedAsync(cancellationToken).ConfigureAwait(false);

        await base.PublishAsync(topic, payload, qosLevel, retain, cancellationToken).ConfigureAwait(false);
    }
}