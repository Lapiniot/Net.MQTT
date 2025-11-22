namespace Net.Mqtt.Server;

public static class RuntimeOptions
{
    public const string MetricsCollectionSupportedSwitchName = "Net.Mqtt.Server.MetricsCollection.IsSupported";

    [FeatureSwitchDefinition(MetricsCollectionSupportedSwitchName)]
    public static bool MetricsCollectionSupported { get; } =
        !AppContext.TryGetSwitch(MetricsCollectionSupportedSwitchName, out var isEnabled) || isEnabled;
}