using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Protocol.V5
{
    public class ServerSession : V4.ServerSession
    {
        public ServerSession(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionState> stateProvider, IMqttServer server) :
            base(transport, reader, stateProvider, server) {}
    }
}