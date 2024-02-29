namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    private Task pingWorker;

    private async Task RunPingWorkerAsync(TimeSpan period, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            Post(PacketFlags.PingReqPacket);
        }
    }
}