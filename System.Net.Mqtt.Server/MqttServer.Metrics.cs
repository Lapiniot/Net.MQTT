using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IProvidePerformanceMetrics
{
    private long totalBytesReceived;
    private long totalPacketsReceived;
    private readonly long[] totalBytesReceivedStats = new long[16];
    private readonly long[] totalPacketsReceivedStats = new long[16];

    internal long TotalBytesReceived => totalBytesReceived;
    internal long TotalPacketsReceived => totalPacketsReceived;
    internal long[] TotalBytesReceivedStats => totalBytesReceivedStats;
    internal long[] TotalPacketsReceivedStats => totalPacketsReceivedStats;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    partial void UpdatePacketMetrics(byte packetType, int totalLength)
    {
        Interlocked.Add(ref totalBytesReceived, totalLength);
        Interlocked.Add(ref totalBytesReceivedStats[packetType], totalLength);
        Interlocked.Increment(ref totalPacketsReceived);
        Interlocked.Increment(ref totalPacketsReceivedStats[packetType]);
    }

    #region IProvidePerformanceMetrics implementation

    IDisposable IProvidePerformanceMetrics.RegisterMeter(string name)
    {
        var meter = new Meter(name ?? GetType().FullName);
        meter.CreateObservableCounter("total-bytes-received", () => totalBytesReceived, "bytes", "Total number of bytes received");
        meter.CreateObservableCounter("total-packets-received", () => totalPacketsReceived, "packets", "Total number of packets received");
        return meter;
    }

    #endregion
}