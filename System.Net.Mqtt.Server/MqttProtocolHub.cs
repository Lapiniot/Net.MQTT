﻿using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    /// <summary>
    /// Represents base abstract MQTT version specific protocol hub implementation which is in charge:
    /// 1. Acts as a client session factory for specific version
    /// 2. Distributes incoming messages from server across internally maintained list of sessions, according to the specific
    /// protocol version rules
    /// </summary>
    public abstract class MqttProtocolHub
    {
        public abstract int ProtocolVersion { get; }

        public abstract Task<MqttServerSession> AcceptConnectionAsync(NetworkTransport transport,
            IObserver<SubscriptionRequest> subscribeObserver, IObserver<MessageRequest> messageObserver,
            CancellationToken cancellationToken);

        public abstract ValueTask DispatchMessageAsync(Message message, CancellationToken cancellationToken);
    }
}