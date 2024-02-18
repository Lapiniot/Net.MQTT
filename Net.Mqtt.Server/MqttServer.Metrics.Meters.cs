using System.Diagnostics.Metrics;

namespace Net.Mqtt.Server;

public sealed partial class MqttServer
{
    private const string TypeKey = "mqtt.packet.type";

    private static readonly KeyValuePair<string, object?>[][] Tags = [
        [new(TypeKey, "NONE")], [new(TypeKey, "CONNECT")], [new(TypeKey, "CONNACK")], [new(TypeKey, "PUBLISH")],
        [new(TypeKey, "PUBACK")], [new(TypeKey, "PUBREC")], [new(TypeKey, "PUBREL")], [new(TypeKey, "PUBCOMP")],
        [new(TypeKey, "SUBSCRIBE")], [new(TypeKey, "SUBACK")], [new(TypeKey, "UNSUBSCRIBE")], [new(TypeKey, "UNSUBACK")],
        [new(TypeKey, "PINGREQ")], [new(TypeKey, "PINGRESP")], [new(TypeKey, "DISCONNECT")], [new(TypeKey, "RESERVED")]
    ];

    private void RegisterMeters(IMeterFactory meterFactory, string name)
    {
        const string Prefix = "mqtt.server.";
        const string PacketUnit = "{packet}";
        const string ByteUnit = "{byte}";
        const string ConnectionUnit = "{connection}";
        const string SessionUnit = "{session}";
        const string SubscriptionUnit = "{subscription}";
        const string PacketsRecName = $"{Prefix}packets_rx";
        const string PacketsSentName = $"{Prefix}packets_tx";
        const string BytesRecName = $"{Prefix}bytes_rx";
        const string BytesSentName = $"{Prefix}bytes_tx";
        const string PacketsRecDesc = "Total number of packets received per packet type";
        const string PacketsSentDesc = "Total number of packets sent per packet type";
        const string BytesRecDesc = "Total number of bytes received per packet type";
        const string BytesSentDesc = "Total number of bytes sent per packet type";

        var meter = meterFactory.Create(new MeterOptions(name));

        #region Data statistics instruments
        meter.CreateObservableGauge<long>($"{Prefix}packets_received", () => new(totalPacketsReceived),
            PacketUnit, "Total number of packets received");
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[1], Tags[1]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[3], Tags[3]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[4], Tags[4]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[5], Tags[5]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[6], Tags[6]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[7], Tags[7]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[8], Tags[8]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[10], Tags[10]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[12], Tags[12]), PacketUnit, PacketsRecDesc);
        meter.CreateObservableGauge<long>(PacketsRecName, () => new(totalPacketsReceivedStats[14], Tags[14]), PacketUnit, PacketsRecDesc);

        meter.CreateObservableGauge<long>($"{Prefix}bytes_received", () => new(totalBytesReceived),
            ByteUnit, "Total number of bytes received");
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[1], Tags[1]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[3], Tags[3]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[4], Tags[4]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[5], Tags[5]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[6], Tags[6]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[7], Tags[7]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[8], Tags[8]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[10], Tags[10]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[12], Tags[12]), ByteUnit, BytesRecDesc);
        meter.CreateObservableGauge<long>(BytesRecName, () => new(totalBytesReceivedStats[14], Tags[14]), ByteUnit, BytesRecDesc);

        meter.CreateObservableGauge<long>($"{Prefix}packets_sent", () => new(totalPacketsSent),
            PacketUnit, "Total number of packets sent");
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[2], Tags[2]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[3], Tags[3]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[4], Tags[4]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[5], Tags[5]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[6], Tags[6]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[7], Tags[7]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[9], Tags[9]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[11], Tags[11]), PacketUnit, PacketsSentDesc);
        meter.CreateObservableGauge<long>(PacketsSentName, () => new(totalPacketsSentStats[13], Tags[13]), PacketUnit, PacketsSentDesc);

        meter.CreateObservableGauge<long>($"{Prefix}bytes_sent", () => new(totalBytesSent),
            ByteUnit, "Total number of bytes sent");
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[2], Tags[2]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[3], Tags[3]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[4], Tags[4]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[5], Tags[5]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[6], Tags[6]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[7], Tags[7]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[9], Tags[9]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[11], Tags[11]), ByteUnit, BytesSentDesc);
        meter.CreateObservableGauge<long>(BytesSentName, () => new(totalBytesSentStats[13], Tags[13]), ByteUnit, BytesSentDesc);
        #endregion

        #region Connection statistics instruments
        meter.CreateObservableGauge($"{Prefix}connections", () => totalConnections,
            ConnectionUnit, "Total connections established");
        meter.CreateObservableGauge($"{Prefix}active_connections", () => activeConnections,
            ConnectionUnit, "Active connections currently running");
        meter.CreateObservableGauge($"{Prefix}rejected_connections", () => rejectedConnections,
            ConnectionUnit, "Total connections rejected");
        #endregion

        #region Session statistics instruments
        meter.CreateObservableGauge($"{Prefix}sessions", () => totalSessions,
            SessionUnit, "Total sessions tracked by the server");
        meter.CreateObservableGauge($"{Prefix}active_sessions", () => activeConnections,
            SessionUnit, "Active sessions currently running");
        #endregion

        #region Subscription statistics instruments
        meter.CreateObservableGauge($"{Prefix}active_subscriptions", () => activeSubscriptions,
            SubscriptionUnit, "Active subscriptions count");
        #endregion
    }
}