using System.Net.Mqtt.Server.Protocol.V3;
using System.Net.Mqtt.Server.Protocol.V5;

namespace System.Net.Mqtt.Server;

public readonly record struct Message3(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, byte QoSLevel, bool Retain);

public readonly record struct Message5(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, byte QoSLevel, bool Retain)
{
    public IReadOnlyList<uint>? SubscriptionIds { get; init; }
    public ReadOnlyMemory<byte> ContentType { get; init; }
    public byte PayloadFormat { get; init; }
    public ReadOnlyMemory<byte> ResponseTopic { get; init; }
    public ReadOnlyMemory<byte> CorrelationData { get; init; }
    public IReadOnlyList<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value)> Properties { get; init; }
    public long? ExpiresAt { get; init; }
}

public readonly record struct IncomingMessage3(Message3 Message, MqttServerSessionState3 Sender);

public readonly record struct IncomingMessage5(Message5 Message, MqttServerSessionState5 Sender);

public readonly record struct PacketRxMessage(byte PacketType, int TotalLength);

public readonly record struct PacketTxMessage(byte PacketType, int TotalLength);

public readonly record struct SubscribeMessage3(IEnumerable<(byte[] Topic, byte QoS)> Filters, MqttServerSessionState3 Sender);

public readonly record struct SubscribeMessage5(IEnumerable<(byte[] Topic, byte QoS)> Filters, MqttServerSessionState5 Sender);

public readonly record struct UnsubscribeMessage(IEnumerable<byte[]> Filters);

public enum ConnectionStatus
{
    Connected,
    Disconnected
}

public readonly record struct ConnectionStateChangedMessage(ConnectionStatus Status, string ClientId);