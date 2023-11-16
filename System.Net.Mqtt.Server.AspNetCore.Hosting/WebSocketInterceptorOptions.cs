using System.ComponentModel.DataAnnotations;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public sealed class WebSocketInterceptorOptions
{
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    public Dictionary<string, string[]> AcceptProtocols { get; } = [];

    [Range(1, int.MaxValue)]
    public int QueueCapacity { get; set; } = 50;
}

#pragma warning disable CA1812

[OptionsValidator]
internal sealed partial class WebSocketInterceptorOptionsValidator : IValidateOptions<WebSocketInterceptorOptions> { }