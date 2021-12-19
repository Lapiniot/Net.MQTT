using System.Buffers;

namespace System.Net.Mqtt.Client;

public partial class MqttClient
{
    private static readonly byte[] PingPacket = new byte[] { 0b1100_0000, 0b0000_0000 };
    private CancelableOperationScope pinger;

    protected override void OnPingResp(byte header, ReadOnlySequence<byte> reminder)
    {
    }

    private async Task StartPingerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(connectionOptions.KeepAlive));
        while(await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            PostRaw(PingPacket);
        }
    }

    protected override void OnPacketSent()
    {
    }
}