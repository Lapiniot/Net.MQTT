namespace Net.Mqtt.Client;

public sealed class DispatchQueueObserver<T> : IObserver<T>
{
    private readonly ChannelReader<T> queueReader;
    private readonly ChannelWriter<T> queueWriter;

    public DispatchQueueObserver() :
        this(Channel.CreateUnbounded<T>())
    {
    }

    public DispatchQueueObserver(Channel<T> channel) =>
        (queueReader, queueWriter) = channel;

    public DispatchQueueObserver(UnboundedChannelOptions options) :
        this(Channel.CreateUnbounded<T>(options))
    {
    }

    public DispatchQueueObserver(BoundedChannelOptions options) :
        this(Channel.CreateBounded<T>(options))
    {
    }

    public ChannelReader<T> QueueReader => queueReader;

    void IObserver<T>.OnCompleted() => queueWriter.Complete();
    void IObserver<T>.OnError(Exception error) { }
    void IObserver<T>.OnNext(T value) => queueWriter.TryWrite(value);
}