using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    private long bytesReceived;
    private long packetsReceived;
    private readonly long[] bytesReceivedStats = new long[16];
    private readonly long[] packetsReceivedStats = new long[16];

    internal long BytesReceived => bytesReceived;
    internal long PacketsReceived => packetsReceived;
    internal long[] BytesReceivedStats => bytesReceivedStats;
    internal long[] PacketsReceivedStats => packetsReceivedStats;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void UpdateReceivedPacketMetrics(byte packetType, int packetSize)
    {
        bytesReceived += packetSize;
        bytesReceivedStats[packetType] += packetSize;
        packetsReceived++;
        packetsReceivedStats[packetType]++;
    }
}