using System.Collections.Concurrent;
using System.Net.Mqtt.Server.Implementations;

namespace System.Net.Mqtt.Server
{
    public sealed partial class MqttServer
    {
        #region ISessionStateProvider<ProtocolStateV3>

        private readonly ConcurrentDictionary<string, SessionStateV3> statesV3;

        SessionStateV3 ISessionStateProvider<SessionStateV3>.Create(string clientId)
        {
            var state = new SessionStateV3();
            return statesV3.AddOrUpdate(clientId, state, (ci, _) => state);
        }

        SessionStateV3 ISessionStateProvider<SessionStateV3>.Get(string clientId)
        {
            return statesV3.TryGetValue(clientId, out var state) ? state : default;
        }

        SessionStateV3 ISessionStateProvider<SessionStateV3>.Remove(string clientId)
        {
            statesV3.TryRemove(clientId, out var state);
            return state;
        }

        #endregion

        #region ISessionStateProvider<ProtocolStateV4>

        SessionStateV4 ISessionStateProvider<SessionStateV4>.Create(string clientId)
        {
            throw new NotImplementedException();
        }

        SessionStateV4 ISessionStateProvider<SessionStateV4>.Get(string clientId)
        {
            throw new NotImplementedException();
        }

        SessionStateV4 ISessionStateProvider<SessionStateV4>.Remove(string clientId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}