using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private readonly Channel<Message> distributionChannel;

        private async Task DispatchMessageAsync(object state, CancellationToken cancellationToken)
        {
            var valueTask = distributionChannel.Reader.ReadAsync(cancellationToken);

            var (topic, payload, qoSLevel, _) = valueTask.IsCompleted
                ? valueTask.Result
                : await valueTask.ConfigureAwait(false);

            Parallel.ForEach(statesV3.Values, parallelOptions, stateV3 =>
            {
                if(stateV3.TopicMatches(topic, out var level))
                {
                    var adjustedQoS = (QoSLevel)Math.Min((byte)qoSLevel, (byte)level);

                    stateV3.EnqueueAsync(new Message(topic, payload, adjustedQoS, false));
                }
            });
        }

        #region IObserver<Message>

        void IObserver<Message>.OnCompleted()
        {
        }

        void IObserver<Message>.OnError(Exception error)
        {
        }

        void IObserver<Message>.OnNext(Message message)
        {
            distributionChannel.Writer.WriteAsync(message);
        }

        #endregion
    }
}