namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    private CancelableOperationScope pingScope;

    protected sealed override void OnPingResp(byte header, ReadOnlySequence<byte> reminder) { }

    private async Task StartPingWorkerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(connectionOptions.KeepAlive));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            Post(PacketFlags.PingReqPacket);
        }
    }
}