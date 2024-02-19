using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

namespace Mqtt.Server.Web;

public sealed class MetricsCollectorOptions
{
    public TimeSpan RecordInterval { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Provides simple implementation of <see cref="IMetricsListener"/> which listens to metrics 
/// emited from system and stores measurements in memory to the typed dictionaries 
/// so they can be accessed by corresponding <see cref="Instrument.Name"/> key.
/// This implementation also provides periodic polling for observable instruments.
/// </summary>
public sealed class MetricsCollector : IMetricsListener, IDisposable
{
    private readonly Dictionary<string, long> longValues = [];
    private readonly Dictionary<string, int> intValues = [];
    private readonly Dictionary<string, decimal> decimalValues = [];
    private readonly Dictionary<string, double> doubleValues = [];
    private readonly IOptionsMonitor<MetricsCollectorOptions> options;
    private PeriodicTimer? timer;
    private Task? recordTask;
    private IObservableInstrumentsSource? source;
    private int instruments;

    public MetricsCollector([NotNull] IOptionsMonitor<MetricsCollectorOptions> options)
    {
        this.options = options;
        options.OnChange(OnOptionsChanged);
    }

    private void OnOptionsChanged(MetricsCollectorOptions options, string? arg2)
    {
        if (timer is not null)
        {
            timer.Period = options.RecordInterval;
        }
    }

    public string Name { get; } = nameof(MetricsCollector);
    public IReadOnlyDictionary<string, long> LongValues => longValues;
    public IReadOnlyDictionary<string, int> IntValues => intValues;
    public IReadOnlyDictionary<string, decimal> DecimalValues => decimalValues;
    public IReadOnlyDictionary<string, double> DoubleValues => doubleValues;

    private void OnLongMeasurement(Instrument instrument, long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        longValues[instrument.Name] = measurement;

    private void OnIntMeasurement(Instrument instrument, int measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        intValues[instrument.Name] = measurement;

    private void OnDoubleMeasurement(Instrument instrument, double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        doubleValues[instrument.Name] = measurement;

    private void OnDecimalMeasurement(Instrument instrument, decimal measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        decimalValues[instrument.Name] = measurement;

    private static async Task RunObserverAsync(PeriodicTimer timer, IObservableInstrumentsSource source)
    {
        source.RecordObservableInstruments();
        while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
        {
            source.RecordObservableInstruments();
        }
    }

    MeasurementHandlers IMetricsListener.GetMeasurementHandlers() => new()
    {
        LongHandler = OnLongMeasurement,
        IntHandler = OnIntMeasurement,
        DecimalHandler = OnDecimalMeasurement,
        DoubleHandler = OnDoubleMeasurement
    };

    void IMetricsListener.Initialize(IObservableInstrumentsSource source)
    {
        this.source = source;
        if (instruments > 0 && recordTask is null)
        {
            Start();
        }
    }

    bool IMetricsListener.InstrumentPublished(Instrument instrument, out object? userState)
    {
        if (instrument.IsObservable && ++instruments > 0 && source is not null && recordTask is null)
        {
            Start();
        }

        userState = null;
        return true;
    }

    void IMetricsListener.MeasurementsCompleted(Instrument instrument, object? userState)
    {
        if (instrument.IsObservable && --instruments is 0)
        {
            Stop();
        }
    }

    public void Dispose() => timer?.Dispose();

    private void Start()
    {
        timer?.Dispose();
        timer = new(options.CurrentValue.RecordInterval);
        recordTask = RunObserverAsync(timer, source!);
    }

    private void Stop()
    {
        timer?.Dispose();
        timer = null;
        recordTask = null;
    }
}