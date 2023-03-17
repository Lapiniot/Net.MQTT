using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IProvideDataStatistics
{
    private long totalBytesReceived;
    private long totalBytesSent;
    private long totalPacketsReceived;
    private long totalPacketsSent;
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

    public long GetPacketsReceived() => totalPacketsReceived;

    public long GetPacketsReceived(PacketType packetType) => totalPacketsReceivedStats[(int)packetType];

    public long GetBytesReceived() => totalBytesReceived;

    public long GetBytesReceived(PacketType packetType) => totalBytesReceivedStats[(int)packetType];

    public long GetPacketsSent() => totalPacketsSent;

    public long GetPacketsSent(PacketType packetType) => totalPacketsSentStats[(int)packetType];

    public long GetBytesSent() => totalBytesSent;

    public long GetBytesSent(PacketType packetType) => totalBytesSentStats[(int)packetType];

    #endregion
}