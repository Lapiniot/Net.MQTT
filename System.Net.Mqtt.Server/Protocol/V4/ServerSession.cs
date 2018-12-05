using System.IO.Pipelines;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class ServerSession : V3.ServerSession
    {
        public ServerSession(INetworkTransport transport, PipeReader reader,
            ISessionStateProvider<SessionState> stateProvider, IMqttServer server) :
            base(transport, reader, stateProvider, server) {}
    }
}