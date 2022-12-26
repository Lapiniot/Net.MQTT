using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

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

    private long totalBytesReceived;
    private long totalPacketsReceived;
    private readonly long[] totalBytesReceivedStats = new long[16];
    private readonly long[] totalPacketsReceivedStats = new long[16];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    partial void UpdatePacketMetrics(byte packetType, int totalLength)
    {
        Interlocked.Add(ref totalBytesReceived, totalLength);
        Interlocked.Add(ref totalBytesReceivedStats[packetType], totalLength);
        Interlocked.Increment(ref totalPacketsReceived);
        Interlocked.Increment(ref totalPacketsReceivedStats[packetType]);
    }

    #region IProvidePerformanceMetrics implementation

    public IDisposable RegisterMeter(string name)
    {
        const string Packets = "packets";
        const string Bytes = "bytes";
        const string TotalPacketsName = "total-packets-received";
        const string TotalBytesName = "total-bytes-received";
        const string TotalPacketsDesc = "Total number of packets received per packet type";
        const string TotalBytesDesc = "Total number of bytes received per packet type";

        var meter = new Meter(name ?? GetType().FullName);

        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[1], tagsMap[1]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[3], tagsMap[3]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[4], tagsMap[4]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[5], tagsMap[5]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[6], tagsMap[6]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[7], tagsMap[7]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[8], tagsMap[8]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[10], tagsMap[10]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[12], tagsMap[12]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge(TotalPacketsName, () => new Measurement<long>(totalPacketsReceivedStats[14], tagsMap[14]), Packets, TotalPacketsDesc);
        meter.CreateObservableGauge("total-packets-received", GetTotalPacketsReceived, Packets, "Total number of packets received");
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[1], tagsMap[1]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[3], tagsMap[3]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[4], tagsMap[4]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[5], tagsMap[5]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[6], tagsMap[6]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[7], tagsMap[7]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[8], tagsMap[8]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[10], tagsMap[10]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[12], tagsMap[12]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge(TotalBytesName, () => new Measurement<long>(totalBytesReceivedStats[14], tagsMap[14]), Bytes, TotalBytesDesc);
        meter.CreateObservableGauge("total-bytes-received", GetTotalBytesReceived, Bytes, "Total number of bytes received");

        return meter;
    }

    private long GetTotalPacketsReceived() => totalPacketsReceived;

    private long GetTotalBytesReceived() => totalBytesReceived;

    #endregion
}