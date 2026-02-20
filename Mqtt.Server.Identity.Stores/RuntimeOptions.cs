using System.Diagnostics.CodeAnalysis;

namespace Mqtt.Server.Identity.Stores;

internal static class RuntimeOptions
{
    private const string MSSQLSupportedSwitchName = "Mqtt.Server.MSSQL.IsSupported";
    private const string CosmosDBSupportedSwitchName = "Mqtt.Server.CosmosDB.IsSupported";
    private const string PostgreSQLSupportedSwitchName = "Mqtt.Server.PostgreSQL.IsSupported";

    [FeatureSwitchDefinition(MSSQLSupportedSwitchName)]
    public static bool MSSQLSupported { get; } = !AppContext.TryGetSwitch(MSSQLSupportedSwitchName, out var isEnabled) || isEnabled;

    [FeatureSwitchDefinition(CosmosDBSupportedSwitchName)]
    public static bool CosmosDBSupported { get; } = !AppContext.TryGetSwitch(CosmosDBSupportedSwitchName, out var isEnabled) || isEnabled;

    [FeatureSwitchDefinition(PostgreSQLSupportedSwitchName)]
    public static bool PostgreSQLSupported { get; } = !AppContext.TryGetSwitch(PostgreSQLSupportedSwitchName, out var isEnabled) || isEnabled;
}