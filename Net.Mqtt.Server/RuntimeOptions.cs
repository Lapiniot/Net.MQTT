namespace Net.Mqtt.Server;

public static class RuntimeOptions
{
    public const string MetricsCollectionSupportedSwitchName = "Net.Mqtt.Server.MetricsCollection.IsSupported";

#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition(MetricsCollectionSupportedSwitchName)]
#endif
    public static bool MetricsCollectionSupported { get; } =
        !AppContext.TryGetSwitch(MetricsCollectionSupportedSwitchName, out var isEnabled) || isEnabled;
}