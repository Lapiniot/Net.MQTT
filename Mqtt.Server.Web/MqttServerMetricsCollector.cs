using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using OOs.Extensions.Diagnostics;

namespace Mqtt.Server.Web;

public sealed class MqttServerMetricsCollector : MetricsCollector
{
    public const string OptionsName = "MqttServer";

    private readonly IDisposable? optionsChangeTracker;

    public MqttServerMetricsCollector(IOptionsMonitor<MetricsCollectorOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        RecordInterval = optionsMonitor.Get(OptionsName).RecordInterval;
        optionsChangeTracker = optionsMonitor.OnChange(OnOptionsChanged);
    }

    private static void OnMeasurement<T>(Instrument instrument, T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        if (state is MetricRecord<T> rec)
        {
            rec.Value = measurement;
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
        IntHandler = OnMeasurement
    };

    protected override bool InstrumentPublished([NotNull] Instrument instrument, out object? userState)
    {
        if (instrument.Meter.Name is not "Net.Mqtt.Server")
        {
            userState = null;
            return false;
        }

        switch (instrument.Name)
        {
            case "mqtt.server.packets_received":
                userState = PacketsReceived ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.packets_sent":
                userState = PacketsSent ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.bytes_received":
                userState = BytesReceived ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.bytes_sent":
                userState = BytesSent ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.connections":
                userState = Connections ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.active_connections":
                userState = ActiveConnections ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.rejected_connections":
                userState = RejectedConnections ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.sessions":
                userState = Sessions ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.active_sessions":
                userState = ActiveSessions ??= new(instrument.Name, instrument.Description);
                break;
            case "mqtt.server.active_subscriptions":
                userState = ActiveSubscriptions ??= new(instrument.Name, instrument.Description);
                break;
            default:
                userState = null;
                return false;
        }

        ((MetricRecord)userState).Enabled = true;
        return true;
    }

    protected override void MeasurementsCompleted(Instrument instrument, object? userState)
    {
        if (userState is MetricRecord rec)
        {
            rec.Enabled = false;
        }
    }

    public override string Name => nameof(MqttServerMetricsCollector);

    public MetricRecord<int>? ActiveConnections { get; private set; }
    public MetricRecord<int>? ActiveSessions { get; private set; }
    public MetricRecord<int>? ActiveSubscriptions { get; private set; }
    public MetricRecord<long>? BytesReceived { get; private set; }
    public MetricRecord<long>? BytesSent { get; private set; }
    public MetricRecord<long>? Connections { get; private set; }
    public MetricRecord<long>? PacketsReceived { get; private set; }
    public MetricRecord<long>? PacketsSent { get; private set; }
    public MetricRecord<long>? RejectedConnections { get; private set; }
    public MetricRecord<int>? Sessions { get; private set; }
}