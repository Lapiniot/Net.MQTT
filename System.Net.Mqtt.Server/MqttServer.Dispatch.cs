using System.Net.Mqtt.Server.Implementations;
using System.Threading.Channels;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        private readonly Channel<Message> distributionChannel;

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