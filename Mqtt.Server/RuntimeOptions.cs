namespace Mqtt.Server;

internal static class RuntimeOptions
{
    private const string WebUISupportedSwitchName = "Mqtt.Server.WebUI.IsSupported";

#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition(WebUISupportedSwitchName)]
#endif
    public static bool WebUISupported { get; } = !AppContext.TryGetSwitch(WebUISupportedSwitchName, out var isEnabled) || isEnabled;
}