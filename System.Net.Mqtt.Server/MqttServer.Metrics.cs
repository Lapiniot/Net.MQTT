using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IProvideDataStatistics, IProvideConnectionStatistics
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

    #region IProvideDataStatistics implementation

    long IProvideDataStatistics.GetPacketsReceived() => totalPacketsReceived;
    long IProvideDataStatistics.GetPacketsReceived(PacketType packetType) => totalPacketsReceivedStats[(int)packetType];
    long IProvideDataStatistics.GetBytesReceived() => totalBytesReceived;
    long IProvideDataStatistics.GetBytesReceived(PacketType packetType) => totalBytesReceivedStats[(int)packetType];
    long IProvideDataStatistics.GetPacketsSent() => totalPacketsSent;
    long IProvideDataStatistics.GetPacketsSent(PacketType packetType) => totalPacketsSentStats[(int)packetType];
    long IProvideDataStatistics.GetBytesSent() => totalBytesSent;
    long IProvideDataStatistics.GetBytesSent(PacketType packetType) => totalBytesSentStats[(int)packetType];

    #endregion

    #region IProvideDataStatistics implementation

    long IProvideConnectionStatistics.GetTotalConnections() => totalConnections;
    long IProvideConnectionStatistics.GetActiveConnections() => activeConnections;
    long IProvideConnectionStatistics.GetRejectedConnections() => rejectedConnections;

    #endregion
}