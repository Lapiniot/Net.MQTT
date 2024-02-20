using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using OOs.Extensions.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mqtt.Server.Web;

internal sealed class MqttServerMetricsCollector : MetricsCollector
{
    private readonly IDisposable? optionsChangeTracker;

    public MqttServerMetricsCollector(IOptionsMonitor<MetricsCollectorOptions> optionsMonitor)
    {
        RecordInterval = optionsMonitor.CurrentValue.RecordInterval;
        optionsChangeTracker = optionsMonitor.OnChange(OnOptionsChanged);
    }

    private void OnLongMeasurement(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        if (state is Record<long> rec)
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
            optionsChangeTracker?.Dispose();
        base.Dispose(disposing);
    }

    protected override MeasurementHandlers GetMeasurementHandlers() => new() { LongHandler = OnLongMeasurement };

    protected override bool InstrumentPublished(Instrument instrument, out object? userState)
    {
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

        return true;
    }

    protected override void MeasurementsCompleted(Instrument instrument, object? userState)
    {
        if (userState is Record<long> rec)
            rec.Enabled = false;
    }

    public override string Name => nameof(MqttServerMetricsCollector);

    public Record<long>? ActiveConnections { get; private set; }
    public Record<long>? ActiveSessions { get; private set; }
    public Record<long>? ActiveSubscriptions { get; private set; }
    public Record<long>? BytesReceived { get; private set; }
    public Record<long>? BytesSent { get; private set; }
    public Record<long>? Connections { get; private set; }
    public Record<long>? PacketsReceived { get; private set; }
    public Record<long>? PacketsSent { get; private set; }
    public Record<long>? RejectedConnections { get; private set; }
    public Record<long>? Sessions { get; private set; }

    public class Record<T> where T : struct
    {
        internal Record(string name, string? description, bool enabled = true)
        {
            Name = name;
            Description = description;
            Enabled = enabled;
        }

        public string? Description { get; }
        public bool Enabled { get; internal set; }
        public string Name { get; }
        public T Value { get; internal set; }
    }
}