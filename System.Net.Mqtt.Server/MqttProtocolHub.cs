namespace System.Net.Mqtt.Server;

/// <summary>
/// Represents base abstract MQTT version specific protocol hub implementation which is in charge:
/// 1. Acts as a client session factory for specific version
/// 2. Distributes incoming messages from server across internally maintained list of sessions, according to the specific
/// protocol version rules
/// </summary>
public abstract class MqttProtocolHub
{
    public abstract int ProtocolLevel { get; }

    public abstract Task<MqttServerSession> AcceptConnectionAsync(NetworkTransportPipe transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver,
        IObserver<PacketReceivedMessage> packetObserver, CancellationToken cancellationToken);

    public abstract void DispatchMessage(Message message);
}