namespace Net.Mqtt.Server.Features;

public interface ISubscriptionStatisticsFeature
{
    long GetActiveSubscriptions();
}