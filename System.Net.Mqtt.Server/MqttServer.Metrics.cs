namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IDataStatisticsFeature, IConnectionStatisticsFeature, ISubscriptionStatisticsFeature, ISessionStatisticsFeature
{
    private long totalBytesReceived;
    private long totalBytesSent;
    private long totalPacketsReceived;
    private long totalPacketsSent;
    private long totalConnections;
    private int activeConnections;
    private long rejectedConnections;
    private int activeSubscriptions;
    private readonly long[] totalBytesReceivedStats = new long[16];
    private readonly long[] totalBytesSentStats = new long[16];
    private readonly long[] totalPacketsReceivedStats = new long[16];
    private readonly long[] totalPacketsSentStats = new long[16];

    private async Task RunStatsAggregatorAsync(CancellationToken stoppingToken)
    {
        if (RuntimeSettings.MetricsCollectionSupport)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await updateStatsSignal.Task.WaitAsync(stoppingToken).ConfigureAwait(false);
                    updateStatsSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    UpdateSubscriptionMetrics();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateReceivedPacketMetrics(byte packetType, int totalLength)
    {
        Interlocked.Add(ref totalBytesReceived, totalLength);
        Interlocked.Add(ref totalBytesReceivedStats[packetType], totalLength);
        Interlocked.Increment(ref totalPacketsReceived);
        Interlocked.Increment(ref totalPacketsReceivedStats[packetType]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSentPacketMetrics(byte packetType, int totalLength)
    {
        Interlocked.Add(ref totalBytesSent, totalLength);
        Interlocked.Add(ref totalBytesSentStats[packetType], totalLength);
        Interlocked.Increment(ref totalPacketsSent);
        Interlocked.Increment(ref totalPacketsSentStats[packetType]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSubscriptionMetrics()
    {
        var total = 0;
        foreach (var (_, (_, session)) in connections)
        {
            total += session.ActiveSubscriptions;
        }

        activeSubscriptions = total;
    }

    #region IDataStatisticsFeature implementation

    long IDataStatisticsFeature.GetPacketsReceived() => totalPacketsReceived;
    long IDataStatisticsFeature.GetPacketsReceived(PacketType packetType) => totalPacketsReceivedStats[(int)packetType];
    long IDataStatisticsFeature.GetBytesReceived() => totalBytesReceived;
    long IDataStatisticsFeature.GetBytesReceived(PacketType packetType) => totalBytesReceivedStats[(int)packetType];
    long IDataStatisticsFeature.GetPacketsSent() => totalPacketsSent;
    long IDataStatisticsFeature.GetPacketsSent(PacketType packetType) => totalPacketsSentStats[(int)packetType];
    long IDataStatisticsFeature.GetBytesSent() => totalBytesSent;
    long IDataStatisticsFeature.GetBytesSent(PacketType packetType) => totalBytesSentStats[(int)packetType];

    #endregion

    #region IConnectionStatisticsFeature implementation

    long IConnectionStatisticsFeature.GetTotalConnections() => totalConnections;
    int IConnectionStatisticsFeature.GetActiveConnections() => activeConnections;
    long IConnectionStatisticsFeature.GetRejectedConnections() => rejectedConnections;

    #endregion

    #region ISubscriptionStatisticsFeature implementation

    long ISubscriptionStatisticsFeature.GetActiveSubscriptions() => activeSubscriptions;

    #endregion

    #region ISessionStatisticsFeature implementation

    int ISessionStatisticsFeature.GetTotalSessions() => totalSessions;

    int ISessionStatisticsFeature.GetActiveSessions() => activeConnections;

    #endregion
}