namespace System.Net.Mqtt.Server.Features;

public interface IProvideConnectionStatistics
{
    long GetTotalConnections();
    long GetActiveConnections();
    long GetRejectedConnections();
}