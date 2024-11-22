namespace Mqtt.Server;

internal static class RuntimeOptions
{
    private const string WebUISupportedSwitchName = "Mqtt.Server.WebUI.IsSupported";

    [FeatureSwitchDefinition(WebUISupportedSwitchName)]
    public static bool WebUISupported { get; } = !AppContext.TryGetSwitch(WebUISupportedSwitchName, out var isEnabled) || isEnabled;
}