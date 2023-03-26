namespace System.Net.Mqtt.Server;

public record class Observers(
    IObserver<SubscribeMessage> Subscribe,
    IObserver<UnsubscribeMessage> Unsubscribe,
    IObserver<IncomingMessage> IncomingMessage,
    IObserver<PacketRxMessage> PacketRx,
    IObserver<PacketTxMessage> PacketTx);