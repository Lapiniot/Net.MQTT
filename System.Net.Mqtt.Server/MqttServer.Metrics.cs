using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IProvidePerformanceMetrics
{
    private long totalBytesReceived;
    private long totalPacketsReceived;
    private readonly long[] totalBytesReceivedStats = new long[16];
    private readonly long[] totalPacketsReceivedStats = new long[16];
    private static readonly string[] packetTypes = {
        "NONE", "CONNECT", "CONNACK", "PUBLISH",
        "PUBACK", "PUBREC", "PUBREL", "PUBCOMP",
        "SUBSCRIBE", "SUBACK", "UNSUBSCRIBE", "UNSUBACK",
        "PINGREQ", "PINGRESP", "DISCONNECT", "RESERVED"
    };

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
        meter.CreateObservableGauge("total-bytes-received", () => totalBytesReceived, "bytes", "Total number of bytes received");
        meter.CreateObservableGauge("total-packets-received", () => totalPacketsReceived, "packets", "Total number of packets received");
        meter.CreateObservableGauge("total-bytes-received-detailed", CreateMultiDimensionalCounterCallback(totalBytesReceivedStats), "bytes", "Total number of bytes received per packet type");
        meter.CreateObservableGauge("total-packets-received-detailed", CreateMultiDimensionalCounterCallback(totalPacketsReceivedStats), "packets", "Total number of packets received per packet type");
        return meter;

        Func<IEnumerable<Measurement<long>>> CreateMultiDimensionalCounterCallback(long[] source)
        {
            return () =>
            {
                var measurements = new Measurement<long>[14];
                Span<Measurement<long>> span = measurements;
                for (var i = 0; i < measurements.Length; i++)
                {
                    span[i] = new(source[i + 1], new KeyValuePair<string, object>("Type", packetTypes[i + 1]));
                }

                return measurements;
            };
        }
    }

    #endregion
}