﻿using System.Net.Pipes;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class ServerSession : V3.ServerSession
    {
        public ServerSession(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionState> stateProvider, IObserver<Message> observer) :
            base(transport, reader, stateProvider, observer)
        {
        }
    }
}