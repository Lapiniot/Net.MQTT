using System.Policies;

namespace System.Net.Mqtt.Client;

public sealed class MqttClient3 : MqttClient
{
    public MqttClient3(NetworkConnection connection, string clientId, int maxInFlight, IRetryPolicy reconnectPolicy, bool disposeTransport) :
        base(connection, clientId, maxInFlight, reconnectPolicy, disposeTransport) =>
        Verify.ThrowIfNullOrEmpty(clientId);

    public override byte ProtocolLevel => 0x03;

    public override string ProtocolName => "MQIsdp";

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);
        await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
    }
}