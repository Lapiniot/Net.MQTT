using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Mqtt.Server.Protocol.V5;

namespace System.Net.Mqtt.Server;

public readonly record struct IncomingMessage3(MqttServerSessionState3 Sender, Message3 Message);

public readonly record struct IncomingMessage5(MqttServerSessionState5 Sender, Message5 Message);

public readonly record struct PacketRxMessage(byte PacketType, long TotalLength);

public readonly record struct PacketTxMessage(byte PacketType, long TotalLength);

public readonly record struct SubscribeMessage3(MqttServerSessionState3 Sender, IReadOnlyList<(byte[] Filter, byte QoS)> Subscriptions);

public readonly record struct SubscribeMessage5(MqttServerSessionState5 Sender, IReadOnlyList<(byte[] Filter, bool Exists, SubscriptionOptions Options)> Subscriptions);

public readonly record struct UnsubscribeMessage(IEnumerable<byte[]> Filters);

public enum ConnectionStatus
{
    Connected,
    Disconnected
}

public readonly record struct ConnectionStateChangedMessage(ConnectionStatus Status, string ClientId);