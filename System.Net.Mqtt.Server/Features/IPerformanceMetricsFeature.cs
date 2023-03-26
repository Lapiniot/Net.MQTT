namespace System.Net.Mqtt.Server.Features;

public interface IPerformanceMetricsFeature
{
    IDisposable RegisterMeter(string name = null);
}