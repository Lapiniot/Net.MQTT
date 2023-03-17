﻿using System.Diagnostics.Metrics;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IProvidePerformanceMetrics
{
    private static readonly KeyValuePair<string, object>[][] tagsMap =
    {
        new[] { new KeyValuePair<string, object>("Type", "NONE") },
        new[] { new KeyValuePair<string, object>("Type", "CONNECT") },
        new[] { new KeyValuePair<string, object>("Type", "CONNACK") },
        new[] { new KeyValuePair<string, object>("Type", "PUBLISH") },
        new[] { new KeyValuePair<string, object>("Type", "PUBACK") },
        new[] { new KeyValuePair<string, object>("Type", "PUBREC") },
        new[] { new KeyValuePair<string, object>("Type", "PUBREL") },
        new[] { new KeyValuePair<string, object>("Type", "PUBCOMP") },
        new[] { new KeyValuePair<string, object>("Type", "SUBSCRIBE") },
        new[] { new KeyValuePair<string, object>("Type", "SUBACK") },
        new[] { new KeyValuePair<string, object>("Type", "UNSUBSCRIBE") },
        new[] { new KeyValuePair<string, object>("Type", "UNSUBACK") },
        new[] { new KeyValuePair<string, object>("Type", "PINGREQ") },
        new[] { new KeyValuePair<string, object>("Type", "PINGRESP") },
        new[] { new KeyValuePair<string, object>("Type", "DISCONNECT") },
        new[] { new KeyValuePair<string, object>("Type", "RESERVED") },
    };

    #region IProvidePerformanceMetrics implementation

    public IDisposable RegisterMeter(string name)
    {
        const string Packets = "packets";
        const string Bytes = "bytes";
        const string PacketsRecName = "total-packets-RX";
        const string PacketsSentName = "total-packets-TX";
        const string BytesRecName = "total-bytes-RX";
        const string BytesSentName = "total-bytes-TX";
        const string PacketsRecDesc = "Total number of packets received per packet type";
        const string PacketsSentDesc = "Total number of packets sent per packet type";
        const string BytesRecDesc = "Total number of bytes received per packet type";
        const string BytesSentDesc = "Total number of bytes sent per packet type";

        var meter = new Meter(name ?? GetType().FullName);

        meter.CreateObservableGauge("total-packets-RX", GetPacketsReceived, Packets, "Total number of packets received");
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[1], tagsMap[1]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[3], tagsMap[3]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[4], tagsMap[4]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[5], tagsMap[5]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[6], tagsMap[6]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[7], tagsMap[7]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[8], tagsMap[8]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[10], tagsMap[10]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[12], tagsMap[12]), Packets, PacketsRecDesc);
        meter.CreateObservableGauge(PacketsRecName, () => new Measurement<long>(totalPacketsReceivedStats[14], tagsMap[14]), Packets, PacketsRecDesc);

        meter.CreateObservableGauge("total-bytes-RX", GetBytesReceived, Bytes, "Total number of bytes received");
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[1], tagsMap[1]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[3], tagsMap[3]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[4], tagsMap[4]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[5], tagsMap[5]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[6], tagsMap[6]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[7], tagsMap[7]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[8], tagsMap[8]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[10], tagsMap[10]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[12], tagsMap[12]), Bytes, BytesRecDesc);
        meter.CreateObservableGauge(BytesRecName, () => new Measurement<long>(totalBytesReceivedStats[14], tagsMap[14]), Bytes, BytesRecDesc);

        meter.CreateObservableGauge("total-packets-TX", GetPacketsSent, Packets, "Total number of packets sent");
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[2], tagsMap[2]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[3], tagsMap[3]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[4], tagsMap[4]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[5], tagsMap[5]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[6], tagsMap[6]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[7], tagsMap[7]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[9], tagsMap[9]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[11], tagsMap[11]), Packets, PacketsSentDesc);
        meter.CreateObservableGauge(PacketsSentName, () => new Measurement<long>(totalPacketsSentStats[13], tagsMap[13]), Packets, PacketsSentDesc);

        meter.CreateObservableGauge("total-bytes-TX", GetBytesSent, Packets, "Total number of bytes sent");
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[2], tagsMap[2]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[3], tagsMap[3]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[4], tagsMap[4]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[5], tagsMap[5]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[6], tagsMap[6]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[7], tagsMap[7]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[9], tagsMap[9]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[11], tagsMap[11]), Bytes, BytesSentDesc);
        meter.CreateObservableGauge(BytesSentName, () => new Measurement<long>(totalBytesSentStats[13], tagsMap[13]), Bytes, BytesSentDesc);

        return meter;
    }

    #endregion
}