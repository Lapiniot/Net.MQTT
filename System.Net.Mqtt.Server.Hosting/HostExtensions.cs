using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting;

public static class HostExtensions
{
    public static async Task WaitForApplicationStartedAsync(this IHostApplicationLifetime applicationLifetime, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        var tcs = new TaskCompletionSource();
        await using (applicationLifetime.ApplicationStarted.Register(static (state) => ((TaskCompletionSource)state).TrySetResult(), tcs))
        await using (stoppingToken.Register(static (state, token) => ((TaskCompletionSource)state).TrySetCanceled(token), tcs))
        {
            await tcs.Task.ConfigureAwait(false);
        }
    }
}