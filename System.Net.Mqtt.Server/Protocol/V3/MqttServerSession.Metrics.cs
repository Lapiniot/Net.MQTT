using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    private static long totalBytesReceived;
    private static long totalPacketsReceived;
    private static readonly long[] totalBytesReceivedStats = new long[16];
    private static readonly long[] totalPacketsReceivedStats = new long[16];
    private readonly long[] bytesReceivedStats = new long[16];
    private readonly long[] packetsReceivedStats = new long[16];
    private long bytesReceived;
    private long packetsReceived;

    internal static long TotalBytesReceived => totalBytesReceived;
    internal static long TotalPacketsReceived => totalPacketsReceived;
    internal long BytesReceived => bytesReceived;
    internal long PacketsReceived => packetsReceived;
    internal static long[] TotalBytesReceivedStats => totalBytesReceivedStats;
    internal static long[] TotalPacketsReceivedStats => totalPacketsReceivedStats;
    internal long[] BytesReceivedStats => bytesReceivedStats;
    internal long[] PacketsReceivedStats => packetsReceivedStats;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    partial void UpdatePacketMetrics(byte packetType, int totalLength)
    {
        Interlocked.Add(ref totalBytesReceived, totalLength);
        Interlocked.Add(ref totalBytesReceivedStats[packetType], totalLength);
        Interlocked.Increment(ref totalPacketsReceived);
        Interlocked.Increment(ref totalPacketsReceivedStats[packetType]);

        Interlocked.Add(ref bytesReceived, totalLength);
        Interlocked.Add(ref bytesReceivedStats[packetType], totalLength);
        Interlocked.Increment(ref packetsReceived);
        Interlocked.Increment(ref packetsReceivedStats[packetType]);
    }
}