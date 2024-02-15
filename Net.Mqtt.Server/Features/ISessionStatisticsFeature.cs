namespace Net.Mqtt.Server.Features;

public interface ISessionStatisticsFeature
{
    int GetTotalSessions();
    int GetActiveSessions();
}