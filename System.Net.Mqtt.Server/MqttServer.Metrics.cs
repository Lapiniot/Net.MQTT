namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IDataStatisticsFeature, IConnectionStatisticsFeature
{
    private long totalBytesReceived;
    private long totalBytesSent;
    private long totalPacketsReceived;
    private long totalPacketsSent;
    private long totalConnections;
    private long activeConnections;
    private long rejectedConnections;
    private readonly long[] totalBytesReceivedStats = new long[16];
    private readonly long[] totalBytesSentStats = new long[16];
    private readonly long[] totalPacketsReceivedStats = new long[16];
    private readonly long[] totalPacketsSentStats = new long[16];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void UpdateReceivedPacketMetrics(byte packetType, int totalLength)
    {
        Interlocked.Add(ref totalBytesReceived, totalLength);
        Interlocked.Add(ref totalBytesReceivedStats[packetType], totalLength);
        Interlocked.Increment(ref totalPacketsReceived);
        Interlocked.Increment(ref totalPacketsReceivedStats[packetType]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void UpdateSentPacketMetrics(byte packetType, int totalLength)
    {
        Interlocked.Add(ref totalBytesSent, totalLength);
        Interlocked.Add(ref totalBytesSentStats[packetType], totalLength);
        Interlocked.Increment(ref totalPacketsSent);
        Interlocked.Increment(ref totalPacketsSentStats[packetType]);
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
    long IConnectionStatisticsFeature.GetActiveConnections() => activeConnections;
    long IConnectionStatisticsFeature.GetRejectedConnections() => rejectedConnections;

    #endregion
}