namespace System.Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private CancelableOperationScope pingScope;

    private async Task StartPingWorkerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(connectionOptions.KeepAlive));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            Post(PacketFlags.PingReqPacket);
        }
    }
}