using System.Collections.Concurrent;
using static System.DateTime;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class SessionStateProvider : SessionStateProvider<SessionState>
    {
        public SessionStateProvider(ConcurrentDictionary<string, SessionState> storage) : base(storage) {}

        protected override SessionState CreateInstance(string id, bool persistent)
        {
            return new SessionState(id, persistent, UtcNow);
        }
    }
}