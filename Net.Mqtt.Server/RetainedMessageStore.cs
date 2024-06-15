using System.Collections.Concurrent;

namespace Net.Mqtt.Server;

public abstract class RetainedMessageStore<TMessage> where TMessage : IApplicationMessage
{
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, TMessage> store = new(ByteSequenceComparer.Instance);

    protected ConcurrentDictionary<ReadOnlyMemory<byte>, TMessage> Store => store;

    public void Update(TMessage message)
    {
        if (message.Payload.Length == 0)
        {
            store.TryRemove(message.Topic, out _);
        }
        else
        {
            store.AddOrUpdate(message.Topic, static (_, state) => state, static (_, _, state) => state, message);
        }
    }
}