using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting;

public static class HostExtensions
{
    public static async Task WaitForApplicationStartedAsync(this IHostApplicationLifetime applicationLifetime, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        var tcs = new TaskCompletionSource();
        await using(applicationLifetime.ApplicationStarted.Register(() => tcs.TrySetResult()))
        await using(stoppingToken.Register(() => tcs.TrySetCanceled()))
        {
            await tcs.Task.ConfigureAwait(false);
        }
    }
}