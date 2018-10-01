using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Threading.CancellationTokenSource;

namespace System.Net.Mqtt.Client
{
    public delegate void ConnectionAbortedHandler(MqttClient sender);

    public partial class MqttClient
    {
        private int aborted;
        private CancellationTokenSource pingCancelSource;
        private CancellationTokenSource pingDelayResetSource;
        private Task pingTask;
        public event ConnectionAbortedHandler ConnectionAborted;

        public async Task StartPingTaskAsync()
        {
            var delayMilliseconds = ConnectionOptions.KeepAlive * 1000;
            byte[] pingPacket = {(byte)PingReq, 0};
            var cancelToken = pingCancelSource.Token;

            while(!cancelToken.IsCancellationRequested)
            {
                try
                {
                    // TODO: optimize and avoid recreation for cases when linked token source is still actual
                    using(var lts = CreateLinkedTokenSource(cancelToken, pingDelayResetSource.Token))
                    {
                        var token = lts.Token;

                        await Task.Delay(delayMilliseconds, token).ConfigureAwait(false);

                        await SendAsync(pingPacket, token).ConfigureAwait(false);
                    }
                }
                catch(OperationCanceledException)
                {
                    // ignored
                }
                catch(ObjectDisposedException)
                {
                    // ignored
                }
            }
        }

        private void StartPingWorker()
        {
            pingCancelSource = new CancellationTokenSource();
            ArisePingTimer();
            pingTask = Task.Run(StartPingTaskAsync);
        }

        private async Task StopPingWorkerAsync()
        {
            using(pingDelayResetSource)
            using(pingCancelSource)
            {
                pingCancelSource.Cancel();
                pingDelayResetSource.Cancel();

                try
                {
                    await pingTask.ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }

                pingDelayResetSource = null;
                pingCancelSource = null;
            }
        }

        private void ArisePingTimer()
        {
            var oldTokenSource = Interlocked.Exchange(ref pingDelayResetSource, new CancellationTokenSource());

            if(oldTokenSource != null)
            {
                oldTokenSource.Cancel();
                oldTokenSource.Dispose();
            }
        }

        private void OnPingResponsePacket()
        {
            Trace.WriteLine(DateTime.Now.TimeOfDay + ": Ping response from server");
        }

        protected virtual void NotifyConnectionAborted()
        {
            ConnectionAborted?.Invoke(this);
        }
    }
}