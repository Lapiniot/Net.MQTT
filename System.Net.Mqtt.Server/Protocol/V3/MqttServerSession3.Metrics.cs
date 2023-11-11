namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    private long bytesReceived;
    private long packetsReceived;
    private FixedArray16<long> bytesReceivedStats;
    private FixedArray16<long> packetsReceivedStats;

    internal long BytesReceived => bytesReceived;
    internal long PacketsReceived => packetsReceived;
    internal ReadOnlySpan<long> BytesReceivedStats => bytesReceivedStats;
    internal ReadOnlySpan<long> PacketsReceivedStats => packetsReceivedStats;

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