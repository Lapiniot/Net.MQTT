using Microsoft.Extensions.Hosting;

#pragma warning disable CA1034 // Nested types should not be visible

namespace Net.Mqtt.Server.Hosting;

public static class HostExtensions
{
    extension(IHostApplicationLifetime applicationLifetime)
    {
        public async Task WaitForApplicationStartedAsync(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(applicationLifetime);
            var tcs = new TaskCompletionSource();
            await using (applicationLifetime.ApplicationStarted.Register(static state => ((TaskCompletionSource)state).TrySetResult(), tcs))
            {
                await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}