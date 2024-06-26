﻿using Net.Mqtt.Server.Protocol.V3;
using Net.Mqtt.Server.Protocol.V5;

namespace Net.Mqtt.Server;

public readonly record struct Message3(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, QoSLevel QoSLevel, bool Retain) : IApplicationMessage;

public record class Message5(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, QoSLevel QoSLevel, bool Retain) : IApplicationMessage
{
    public long? ExpiresAt { get; init; }
    public bool PayloadFormat { get; init; }
    public ReadOnlyMemory<byte> ContentType { get; init; }
    public ReadOnlyMemory<byte> ResponseTopic { get; init; }
    public ReadOnlyMemory<byte> CorrelationData { get; init; }
    public IReadOnlyList<uint>? SubscriptionIds { get; init; }
    public IReadOnlyList<UserProperty>? UserProperties { get; init; }
}

public readonly record struct IncomingMessage3(MqttServerSessionState3 Sender, Message3 Message);

public readonly record struct IncomingMessage5(MqttServerSessionState5 Sender, Message5 Message);

public readonly record struct PacketRxMessage(PacketType PacketType, int TotalLength);

public readonly record struct PacketTxMessage(PacketType PacketType, int TotalLength);

public readonly record struct SubscribeMessage3(MqttServerSessionState3 Sender, IReadOnlyList<(byte[] Filter, byte QoS)> Subscriptions);

public readonly record struct SubscribeMessage5(MqttServerSessionState5 Sender, IReadOnlyList<(byte[] Filter, bool Exists, SubscriptionOptions Options)> Subscriptions);

public readonly record struct UnsubscribeMessage(IEnumerable<byte[]> Filters);

public enum ConnectionStatus
{
    Connected,
    Disconnected
}

public readonly record struct ConnectionStateChangedMessage(ConnectionStatus Status, string ClientId);