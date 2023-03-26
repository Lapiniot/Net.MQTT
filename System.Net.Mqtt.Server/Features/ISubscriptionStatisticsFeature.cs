namespace System.Net.Mqtt.Server.Features;

public interface ISubscriptionStatisticsFeature
{
    long GetActiveSubscriptionsCount();
}