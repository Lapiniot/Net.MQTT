namespace System.Net.Mqtt.Server.Protocol.V4;

public class MqttServerSession : V3.MqttServerSession
{
    public MqttServerSession(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<V3.MqttServerSessionState> stateRepository, ILogger logger,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver,
        IObserver<PacketRxMessage> packetRxObserver, IObserver<PacketTxMessage> packetTxObserver,
        int maxUnflushedBytes) :
        base(clientId, transport, stateRepository, logger, subscribeObserver, messageObserver, packetRxObserver, packetTxObserver, maxUnflushedBytes)
    { }
}