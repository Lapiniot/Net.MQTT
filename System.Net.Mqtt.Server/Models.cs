namespace System.Net.Mqtt.Server;

public readonly record struct Message(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, byte QoSLevel, bool Retain);

public readonly record struct IncomingMessage(in Message Message, string ClientId);

public readonly record struct PacketRxMessage(byte PacketType, int TotalLength);

public readonly record struct PacketTxMessage(byte PacketType, int TotalLength);

public readonly record struct SubscribeMessage(MqttServerSessionState State, IEnumerable<(byte[] Topic, byte QoS)> Filters);

public readonly record struct UnsubscribeMessage(MqttServerSessionState State, IEnumerable<byte[]> Filters);

public enum ConnectionStatus
{
    Connected,
    Disconnected,
    Aborted
}

public readonly record struct ConnectionStateChangedMessage(ConnectionStatus Status, string ClientId);