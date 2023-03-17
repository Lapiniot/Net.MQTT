namespace System.Net.Mqtt.Server;

public interface IProvideConnectionStatistics
{
    long GetTotalConnections();
    long GetActiveConnections();
    long GetRejectedConnections();
}