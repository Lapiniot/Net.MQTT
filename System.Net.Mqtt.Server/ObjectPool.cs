using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public sealed class ObjectPool<T> where T : new()
{
    private readonly ConcurrentQueue<T> store = new();
    private readonly int maxInstances;
    private int instances;

    public ObjectPool(int maxInstances = 50)
    {
        this.maxInstances = maxInstances;
    }

    private static readonly Lazy<ObjectPool<T>> instance = new(() => new ObjectPool<T>(), LazyThreadSafetyMode.ExecutionAndPublication);

#pragma warning disable CA1000
    public static ObjectPool<T> Shared => instance.Value;
#pragma warning restore CA100

    public T Rent()
    {
        return store.TryDequeue(out T instance) ? instance : new T();
    }

    public void Return(T instance)
    {
        store.Enqueue(instance);
    }
}
