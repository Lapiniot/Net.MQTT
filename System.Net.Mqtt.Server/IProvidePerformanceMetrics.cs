namespace System.Net.Mqtt.Server;

public interface IProvidePerformanceMetrics
{
    IDisposable RegisterMeter(string name = null);
}