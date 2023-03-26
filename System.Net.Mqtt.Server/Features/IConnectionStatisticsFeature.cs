namespace System.Net.Mqtt.Server.Features;

public interface IConnectionStatisticsFeature
{
    long GetTotalConnections();
    long GetActiveConnections();
    long GetRejectedConnections();
}