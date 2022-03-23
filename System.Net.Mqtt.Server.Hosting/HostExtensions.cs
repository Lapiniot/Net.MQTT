using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting;

public static class HostExtensions
{
    public static async Task WaitForApplicationStartedAsync(this IHostApplicationLifetime applicationLifetime, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        var tcs = new TaskCompletionSource();
        await using (applicationLifetime.ApplicationStarted.Register(static state => ((TaskCompletionSource)state).TrySetResult(), tcs))
        {
            await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}