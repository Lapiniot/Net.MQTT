using System.ComponentModel.DataAnnotations;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

public sealed class WebSocketConnectionOptions : WebSocketOptions
{
    [MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    public IList<string> SubProtocols { get; } = [];
    public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

#pragma warning disable CA1812

[OptionsValidator]
internal sealed partial class WebSocketConnectionOptionsValidator :
    IValidateOptions<WebSocketConnectionOptions>
{ }