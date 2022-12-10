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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    partial void UpdatePacketMetrics(byte packetType, int totalLength)
    {
        bytesReceived += totalLength;
        bytesReceivedStats[packetType] += totalLength;
        packetsReceived++;
        packetsReceivedStats[packetType]++;

        packetObserver.OnNext(new(packetType, totalLength));
    }
}