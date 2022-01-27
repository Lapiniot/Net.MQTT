using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting;

public partial class GenericMqttHostService
{
    private readonly ILogger<GenericMqttHostService> logger;

    [LoggerMessage(1, LogLevel.Error, "Error running MQTT server instance")]
    private partial void LogError(Exception exception);

    [LoggerMessage(2, LogLevel.Information, "Starting hosted MQTT service...")]
    private partial void LogStarting();

    [LoggerMessage(3, LogLevel.Information, "Started hosted MQTT service")]
    private partial void LogStarted();

    [LoggerMessage(4, LogLevel.Information, "Stopping hosted MQTT service...")]
    private partial void LogStopping();

    [LoggerMessage(5, LogLevel.Information, "Stopped hosted MQTT service")]
    private partial void LogStopped();
}