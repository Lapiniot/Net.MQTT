using System.Policies;

namespace System.Net.Mqtt.Client;

public sealed class MqttClient3 : MqttClient3Core
{
    public MqttClient3(NetworkConnection connection, string clientId, int maxInFlight, IRetryPolicy reconnectPolicy, bool disposeTransport) :
        base(connection, clientId, maxInFlight, reconnectPolicy, disposeTransport, protocolLevel: 0x03, protocolName: "MQIsdp") =>
        ArgumentException.ThrowIfNullOrEmpty(clientId);

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);
        await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
    }
}