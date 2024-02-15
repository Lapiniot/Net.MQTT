namespace Net.Mqtt.Server;

public static class RuntimeSettings
{
    private static readonly bool metricsCollectionSupport = !AppContext.TryGetSwitch("Net.Mqtt.Server.MetricsCollectionSupport", out var isEnabled) || isEnabled;

    public static bool MetricsCollectionSupport => metricsCollectionSupport;
}