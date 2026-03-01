using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using OOs.Extensions.Diagnostics;

namespace Mqtt.Server.Web.Metrics;

public sealed class MqttServerMetricsCollector : MetricsCollector
{
    public const string OptionsName = "MqttServer";

    private readonly IDisposable? optionsChangeTracker;
    private MetricSnapshot<long>? rejectedConnections;
    private MetricSnapshot<int>? sessions;
    private MetricSnapshot<long>? bytesSent;
    private MetricSnapshot<long>? connections;
    private MetricSnapshot<long>? bytesReceived;
    private MetricSnapshot<long>? packetsReceived;
    private MetricSnapshot<long>? packetsSent;
    private MetricSnapshot<int>? activeConnections;
    private MetricSnapshot<int>? activeSessions;
    private MetricSnapshot<int>? activeSubscriptions;
    private CpuTimeMetricSnapshot? cpuTime;
    private MetricSnapshot<long>? workingSet;
    private MetricSnapshot<long>? totalAllocatedBytes;
    private MetricSnapshot<long>? totalCommittedBytes;
    private GcHeapSizeMetricSnapshot? heapSize;
    private GcHeapSizeMetricSnapshot? fragmentationAfterBytes;
    private MetricSnapshot<double>? totalPauseDuration;
    private MetricSnapshot<long>? threadCount;
    private MetricSnapshot<long>? completedWorkItemCount;
    private MetricSnapshot<long>? pendingWorkItemCount;
    private MetricSnapshot<long>? lockContentionCount;
    private MetricSnapshot<long>? timerActiveCount;
    private MetricSnapshot<long>? exceptionCount;
    private GcCollectionMetricSnapshot? gcCollections;

    public MqttServerMetricsCollector(IOptionsMonitor<MetricsCollectorOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        RecordInterval = optionsMonitor.Get(OptionsName).RecordInterval;
        optionsChangeTracker = optionsMonitor.OnChange(OnOptionsChanged);
    }

    public override string Name => nameof(MqttServerMetricsCollector);

    public MetricSnapshot<int>? ActiveConnections => activeConnections;
    public MetricSnapshot<int>? ActiveSessions => activeSessions;
    public MetricSnapshot<int>? ActiveSubscriptions => activeSubscriptions;
    public MetricSnapshot<long>? BytesReceived => bytesReceived;
    public MetricSnapshot<long>? BytesSent => bytesSent;
    public MetricSnapshot<long>? Connections => connections;
    public MetricSnapshot<long>? PacketsReceived => packetsReceived;
    public MetricSnapshot<long>? PacketsSent => packetsSent;
    public MetricSnapshot<long>? RejectedConnections => rejectedConnections;
    public MetricSnapshot<int>? Sessions => sessions;
    public CpuTimeMetricSnapshot? CpuTime => cpuTime;
    public MetricSnapshot<long>? WorkingSet => workingSet;
    public MetricSnapshot<long>? TotalAllocatedBytes => totalAllocatedBytes;
    public MetricSnapshot<long>? TotalCommittedBytes => totalCommittedBytes;
    public GcHeapSizeMetricSnapshot? HeapSize => heapSize;
    public GcHeapSizeMetricSnapshot? FragmentationAfterBytes => fragmentationAfterBytes;
    public MetricSnapshot<double>? TotalPauseDuration => totalPauseDuration;
    public MetricSnapshot<long>? ThreadCount => threadCount;
    public MetricSnapshot<long>? CompletedWorkItemCount => completedWorkItemCount;
    public MetricSnapshot<long>? PendingWorkItemCount => pendingWorkItemCount;
    public MetricSnapshot<long>? LockContentionCount => lockContentionCount;
    public MetricSnapshot<long>? TimerActiveCount => timerActiveCount;
    public MetricSnapshot<long>? ExceptionCount => exceptionCount;
    public GcCollectionMetricSnapshot? GcCollections => gcCollections;

    private static void OnMeasurement<T>(Instrument instrument, T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) where T : struct
    {
        if (state is Metric<T> record)
        {
            record.Update(measurement, tags);
        }
    }

    private void OnOptionsChanged(MetricsCollectorOptions options)
    {
        RecordInterval = options.RecordInterval;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            optionsChangeTracker?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override MeasurementHandlers GetMeasurementHandlers() => new()
    {
        LongHandler = OnMeasurement,
        IntHandler = OnMeasurement,
        DoubleHandler = OnMeasurement
    };

    protected override void Initialize(IObservableInstrumentsSource source) => base.Initialize(source);

    protected override bool InstrumentPublished([NotNull] Instrument instrument, out object? userState)
    {
        if (instrument.Meter.Name is "Net.Mqtt.Server")
        {
            switch (instrument.Name)
            {
                case "mqtt.server.packets_received":
                    userState = LazyInit(ref packetsReceived, instrument);
                    break;
                case "mqtt.server.packets_sent":
                    userState = LazyInit(ref packetsSent, instrument);
                    break;
                case "mqtt.server.bytes_received":
                    userState = LazyInit(ref bytesReceived, instrument);
                    break;
                case "mqtt.server.bytes_sent":
                    userState = LazyInit(ref bytesSent, instrument);
                    break;
                case "mqtt.server.connections":
                    userState = LazyInit(ref connections, instrument);
                    break;
                case "mqtt.server.active_connections":
                    userState = LazyInit(ref activeConnections, instrument);
                    break;
                case "mqtt.server.rejected_connections":
                    userState = LazyInit(ref rejectedConnections, instrument);
                    break;
                case "mqtt.server.sessions":
                    userState = LazyInit(ref sessions, instrument);
                    break;
                case "mqtt.server.active_sessions":
                    userState = LazyInit(ref activeSessions, instrument);
                    break;
                case "mqtt.server.active_subscriptions":
                    userState = LazyInit(ref activeSubscriptions, instrument);
                    break;
                default:
                    userState = null;
                    return false;
            }

            ((Metric)userState).Enabled = true;
            return true;
        }
        else if (instrument.Meter.Name is "System.Runtime")
        {
            switch (instrument.Name)
            {
                case "dotnet.process.cpu.time":
                    userState = (cpuTime ??= new(instrument.Name, instrument.Description));
                    break;
                case "dotnet.process.memory.working_set":
                    userState = LazyInit(ref workingSet, instrument);
                    break;
                case "dotnet.gc.heap.total_allocated":
                    userState = LazyInit(ref totalAllocatedBytes, instrument);
                    break;
                case "dotnet.gc.collections":
                    userState = (gcCollections ??= new(instrument.Name, instrument.Description));
                    break;
                case "dotnet.gc.last_collection.memory.committed_size":
                    userState = LazyInit(ref totalCommittedBytes, instrument);
                    break;
                case "dotnet.gc.last_collection.heap.size":
                    userState = (heapSize ??= new(instrument.Name, instrument.Description));
                    break;
                case "dotnet.gc.last_collection.heap.fragmentation.size":
                    userState = (fragmentationAfterBytes ??= new(instrument.Name, instrument.Description));
                    break;
                case "dotnet.gc.pause.time":
                    userState = LazyInit(ref totalPauseDuration, instrument);
                    break;
                case "dotnet.thread_pool.thread.count":
                    userState = LazyInit(ref threadCount, instrument);
                    break;
                case "dotnet.thread_pool.work_item.count":
                    userState = LazyInit(ref completedWorkItemCount, instrument);
                    break;
                case "dotnet.thread_pool.queue.length":
                    userState = LazyInit(ref pendingWorkItemCount, instrument);
                    break;
                case "dotnet.monitor.lock_contentions":
                    userState = LazyInit(ref lockContentionCount, instrument);
                    break;
                case "dotnet.timer.count":
                    userState = LazyInit(ref timerActiveCount, instrument);
                    break;
                case "dotnet.exceptions":
                    userState = LazyInit(ref exceptionCount, instrument);
                    break;
                default:
                    userState = null;
                    return false;
            }

            ((Metric)userState).Enabled = true;
            return true;
        }
        else
        {
            userState = null;
            return false;
        }

        static MetricSnapshot<T> LazyInit<T>(ref MetricSnapshot<T>? metric, Instrument instrument) where T : struct
        {
            metric ??= new MetricSnapshot<T>(instrument.Name, instrument.Description);
            return metric;
        }
    }

    protected override void MeasurementsCompleted(Instrument instrument, object? userState)
    {
        if (userState is Metric rec)
        {
            rec.Enabled = false;
        }
    }
}