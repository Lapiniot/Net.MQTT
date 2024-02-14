namespace System.Net.Mqtt.Client;

public sealed class MqttClientDispatchQueueObserver : IObserver<MqttMessage>
{
    private readonly ChannelReader<MqttMessage> queueReader;
    private readonly ChannelWriter<MqttMessage> queueWriter;

    public MqttClientDispatchQueueObserver() :
        this(Channel.CreateUnbounded<MqttMessage>())
    {
    }

    public MqttClientDispatchQueueObserver(UnboundedChannelOptions options) :
        this(Channel.CreateUnbounded<MqttMessage>(options))
    {
    }

    public MqttClientDispatchQueueObserver(BoundedChannelOptions options) :
        this(Channel.CreateBounded<MqttMessage>(options))
    {
    }

    private MqttClientDispatchQueueObserver(Channel<MqttMessage> channel) =>
        (queueReader, queueWriter) = channel;

    public ChannelReader<MqttMessage> QueueReader => queueReader;

    void IObserver<MqttMessage>.OnCompleted() => queueWriter.Complete();
    void IObserver<MqttMessage>.OnError(Exception error) { }
    void IObserver<MqttMessage>.OnNext(MqttMessage value) => queueWriter.TryWrite(value);
}