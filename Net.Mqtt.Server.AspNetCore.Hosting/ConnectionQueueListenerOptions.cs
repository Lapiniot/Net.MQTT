using System.ComponentModel.DataAnnotations;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public sealed class ConnectionQueueListenerOptions
{
    [Range(1, int.MaxValue)]
    public int QueueCapacity { get; set; } = 50;
}

#pragma warning disable CA1812

[OptionsValidator]
internal sealed partial class ConnectionQueueListenerOptionsValidator :
    IValidateOptions<ConnectionQueueListenerOptions>
{ }