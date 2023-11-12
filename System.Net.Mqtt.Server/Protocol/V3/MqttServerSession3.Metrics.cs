namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    private long bytesReceived;
    private long packetsReceived;
    private readonly long[] bytesReceivedStats = new long[16];
    private readonly long[] packetsReceivedStats = new long[16];

    internal long BytesReceived => bytesReceived;
    internal long PacketsReceived => packetsReceived;
    internal long[] BytesReceivedStats => bytesReceivedStats;
    internal long[] PacketsReceivedStats => packetsReceivedStats;

    partial void UpdateReceivedPacketMetrics(PacketType packetType, int packetSize)
    {
        // Ensure value is in the 0..15 range to eliminate bounds check
        var index = (int)packetType & 0x0f;
        bytesReceived += packetSize;
        bytesReceivedStats[index] += packetSize;
        packetsReceived++;
        packetsReceivedStats[index]++;
    }
}