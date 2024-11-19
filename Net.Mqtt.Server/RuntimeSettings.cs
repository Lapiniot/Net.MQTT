namespace Net.Mqtt.Server;

public static class RuntimeSettings
{
    public const string MetricsCollectionSupportFeatureName = "Net.Mqtt.Server.MetricsCollectionSupport";

#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition(MetricsCollectionSupportFeatureName)]
#endif
    public static bool MetricsCollectionSupport { get; } =
        !AppContext.TryGetSwitch(MetricsCollectionSupportFeatureName, out var isEnabled) || isEnabled;
}