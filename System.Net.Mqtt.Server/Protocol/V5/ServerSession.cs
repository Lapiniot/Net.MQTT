using System.IO.Pipelines;

namespace System.Net.Mqtt.Server.Protocol.V5
{
    public class ServerSession : V4.ServerSession
    {
        public ServerSession(INetworkTransport transport, PipeReader reader,
            ISessionStateProvider<SessionState> stateProvider, IMqttServer server) :
            base(transport, reader, stateProvider, server) {}
    }
}