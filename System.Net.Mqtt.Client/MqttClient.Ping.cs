using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.PacketType;
using static System.Net.Sockets.SocketFlags;
using static System.Threading.CancellationTokenSource;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private CancellationTokenSource pingDelayResetSource;
        private CancellationTokenSource pingCancelSource;
        private Task pingTask;

        public async Task StartPingTaskAsync()
        {
            int delayMilliseconds = Options.KeepAlive * 1000;
            byte[] pingPacket = new byte[] { (byte)PingReq, 0 };
            CancellationToken cancelToken = pingCancelSource.Token;

            while(!cancelToken.IsCancellationRequested)
            {
                try
                {
                    using(var lts = CreateLinkedTokenSource(cancelToken, pingDelayResetSource.Token))
                    {
                        var token = lts.Token;

                        await Task.Delay(delayMilliseconds, token).ConfigureAwait(false);

                        await Socket.SendAsync(pingPacket, token).ConfigureAwait(false);
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
            pingCancelSource.Cancel();
            pingCancelSource.Dispose();
            pingCancelSource = null;

            pingDelayResetSource.Dispose();
            pingDelayResetSource = null;

            try
            {
                await pingTask.ConfigureAwait(false);
            }
            catch
            {
                // ignored
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
    }
}