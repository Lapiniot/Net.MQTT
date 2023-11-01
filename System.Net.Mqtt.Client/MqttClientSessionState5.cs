namespace System.Net.Mqtt.Client;

public sealed class MqttClientSessionState5 : MqttSessionState<Message5>
{
    public ushort CreateMessageDeliveryState(Message5 message) => CreateDeliveryStateCore(message);
    public bool DiscardMessageDeliveryState(ushort id) => DiscardDeliveryStateCore(id);
}