namespace System.Net.Mqtt.Server.Features;

public interface IProvidePerformanceMetrics
{
    IDisposable RegisterMeter(string name = null);
}