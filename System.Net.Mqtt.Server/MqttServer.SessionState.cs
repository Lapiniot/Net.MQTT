using System.Collections.Concurrent;
using System.Net.Mqtt.Server.Implementations;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        #region ISessionStateProvider<ProtocolStateV3>

        private readonly ConcurrentDictionary<string, SessionStateV3> statesV3;

        void ISessionStateProvider<SessionStateV3>.Remove(string clientId)
        {
            if(statesV3.TryRemove(clientId, out var state)) state?.Dispose();
        }

        SessionStateV3 ISessionStateProvider<SessionStateV3>.GetOrCreate(string clientId, bool clean)
        {
            return !clean
                ? statesV3.GetOrAdd(clientId, id => new SessionStateV3(true))
                : statesV3.AddOrUpdate(clientId, _ => new SessionStateV3(false),
                    (_, existing) =>
                    {
                        existing.Dispose();
                        return new SessionStateV3(false);
                    });
        }

        #endregion

        #region ISessionStateProvider<ProtocolStateV4>

        void ISessionStateProvider<SessionStateV4>.Remove(string clientId)
        {
            throw new NotImplementedException();
        }

        SessionStateV4 ISessionStateProvider<SessionStateV4>.GetOrCreate(string clientId, bool clean)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}