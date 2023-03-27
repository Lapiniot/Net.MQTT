namespace System.Net.Mqtt.Server.Features;

public interface IConnectionStatisticsFeature
{
    long GetTotalConnections();
    int GetActiveConnections();
    long GetRejectedConnections();
}