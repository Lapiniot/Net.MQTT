namespace Mqtt.Server;

internal static class RuntimeOptions
{
    private const string WebUISupportedSwitchName = "Mqtt.Server.WebUI.IsSupported";
    private const string MSSQLSupportedSwitchName = "Mqtt.Server.MSSQL.IsSupported";

    [FeatureSwitchDefinition(WebUISupportedSwitchName)]
    public static bool WebUISupported { get; } = !AppContext.TryGetSwitch(WebUISupportedSwitchName, out var isEnabled) || isEnabled;

    [FeatureSwitchDefinition(MSSQLSupportedSwitchName)]
    public static bool MSSQLSupported { get; } = !AppContext.TryGetSwitch(MSSQLSupportedSwitchName, out var isEnabled) || isEnabled;
}