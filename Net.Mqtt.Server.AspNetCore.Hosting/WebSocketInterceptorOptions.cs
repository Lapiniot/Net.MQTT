using System.ComponentModel.DataAnnotations;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public sealed class WebSocketInterceptorOptions
{
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    public Dictionary<string, string[]> AcceptProtocols { get; } = [];
}

#pragma warning disable CA1812

[OptionsValidator]
internal sealed partial class WebSocketInterceptorOptionsValidator :
    IValidateOptions<WebSocketInterceptorOptions>
{ }