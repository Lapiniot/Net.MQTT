namespace Mqtt.Server.Web;

public sealed class UIOptions
{
    public TimeSpan AutoRefreshInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan EventsThrottleInterval { get; set; } = TimeSpan.FromSeconds(5);
}