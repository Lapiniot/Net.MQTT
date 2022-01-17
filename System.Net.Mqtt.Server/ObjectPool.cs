using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server;

public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> store = new();

    public ObjectPool()
    {
    }

#pragma warning disable CA1000

    public static ObjectPool<T> Shared => InstanceHolder.instance;

#pragma warning restore CA1000

    public T Rent()
    {
        return store.TryTake(out var value) ? value : new T();
    }

    public void Return(T instance)
    {
        store.Add(instance);
    }

    private class InstanceHolder
    {
        static InstanceHolder() { }
        internal static readonly ObjectPool<T> instance = new();
    }
}
