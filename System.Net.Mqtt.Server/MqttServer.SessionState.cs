using System.Collections.Concurrent;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        #region ISessionStateProvider<V3.SessionState>

        private readonly ConcurrentDictionary<string, Protocol.V3.SessionState> statesV3;

        void ISessionStateProvider<Protocol.V3.SessionState>.Remove(string clientId)
        {
            if(statesV3.TryRemove(clientId, out var state)) state?.Dispose();
        }

        Protocol.V3.SessionState ISessionStateProvider<Protocol.V3.SessionState>.GetOrCreate(string clientId, bool clean)
        {
            return !clean
                ? statesV3.GetOrAdd(clientId, id => new Protocol.V3.SessionState(true))
                : statesV3.AddOrUpdate(clientId, _ => new Protocol.V3.SessionState(false),
                    (_, existing) =>
                    {
                        existing.Dispose();
                        return new Protocol.V3.SessionState(false);
                    });
        }

        #endregion

        #region ISessionStateProvider<V4.SessionState>

        void ISessionStateProvider<Protocol.V4.SessionState>.Remove(string clientId)
        {
            throw new NotImplementedException();
        }

        Protocol.V4.SessionState ISessionStateProvider<Protocol.V4.SessionState>.GetOrCreate(string clientId, bool clean)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}