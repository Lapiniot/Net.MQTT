using System.Policies;

namespace System.Net.Mqtt.Client;

public sealed class MqttClient4 : MqttClient
{
    public MqttClient4(NetworkTransport transport, string clientId = null,
        ISessionStateRepository<MqttClientSessionState> repository = null,
        IRetryPolicy reconnectPolicy = null) :
        base(transport, clientId, repository, reconnectPolicy)
    {
    }

    public override byte ProtocolLevel => 0x04;

    public override string ProtocolName => "MQTT";

    public async Task ConnectAsync(MqttConnectionOptions options, bool waitForAcknowledgement, CancellationToken cancellationToken = default)
    {
        await ConnectAsync(options, cancellationToken).ConfigureAwait(false);

        if(waitForAcknowledgement)
        {
            var valueTask = WaitConnAckAsync(cancellationToken);
            if(!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.ConfigureAwait(false);
            }
        }
    }

    public override async Task<byte[]> SubscribeAsync((string topic, QoSLevel qos)[] topics, CancellationToken cancellationToken = default)
    {
        if(!ConnectionAcknowledged)
        {
            await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
        }

        return await base.SubscribeAsync(topics, cancellationToken).ConfigureAwait(false);
    }

    public override async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
    {
        if(!ConnectionAcknowledged)
        {
            await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
        }

        await base.UnsubscribeAsync(topics, cancellationToken).ConfigureAwait(false);
    }

    public override async Task PublishAsync(string topic, Memory<byte> payload, QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
    {
        if(qosLevel != QoSLevel.QoS0 && !ConnectionAcknowledged)
        {
            await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
        }

        await base.PublishAsync(topic, payload, qosLevel, retain, cancellationToken).ConfigureAwait(false);
    }

    public override void Publish(string topic, Memory<byte> payload, QoSLevel qosLevel = QoSLevel.AtMostOnce, bool retain = false)
    {
        if(qosLevel != QoSLevel.QoS0 && !ConnectionAcknowledged)
        {
            throw new InvalidOperationException("Cannot send QoS1 or QoS2 messages until connection is acknowledged by the server");
        }

        base.Publish(topic, payload, qosLevel, retain);
    }
}