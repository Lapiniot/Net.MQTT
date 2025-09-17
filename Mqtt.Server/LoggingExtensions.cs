namespace Mqtt.Server;

internal static partial class LoggingExtensions
{
    [LoggerMessage(LogLevel.Error, "Migrations operations are not supported with NativeAOT."
        + " Use a migration bundle or an alternate way of executing migration operations.")]
    public static partial void LogMigrationsNotSupportedWithAOT(this ILogger logger);
}