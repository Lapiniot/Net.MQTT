using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server
{
    public interface ISessionStateProvider<out T> where T : SessionState
    {
        void Remove(string clientId);
        T GetOrCreate(string clientId, bool clean);
    }

    public abstract class SessionStateProvider<T> : ISessionStateProvider<T> where T : SessionState
    {
        private readonly ConcurrentDictionary<string, T> storage;

        protected SessionStateProvider(ConcurrentDictionary<string, T> storage)
        {
            this.storage = storage;
        }

        public void Remove(string clientId)
        {
            if(storage.TryRemove(clientId, out var state)) state?.Dispose();
        }

        public T GetOrCreate(string clientId, bool clean)
        {
            return !clean
                ? storage.GetOrAdd(clientId, id => CreateInstance(id, true))
                : storage.AddOrUpdate(clientId, id => CreateInstance(id, false),
                    (id, existing) =>
                    {
                        existing.Dispose();
                        return CreateInstance(id, false);
                    });
        }

        protected abstract T CreateInstance(string id, bool persistent);
    }
}