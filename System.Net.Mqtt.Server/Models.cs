namespace System.Net.Mqtt.Server;

public readonly record struct Message3(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, byte QoSLevel, bool Retain);

public readonly record struct Message5(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, byte QoSLevel, bool Retain);

public readonly record struct IncomingMessage3(in Message3 Message, string ClientId);

public readonly record struct IncomingMessage5(in Message5 Message, string ClientId);

public readonly record struct PacketRxMessage(byte PacketType, int TotalLength);

public readonly record struct PacketTxMessage(byte PacketType, int TotalLength);

public readonly record struct SubscribeMessage3(ChannelWriter<Message3> QueueWriter, IEnumerable<(byte[] Topic, byte QoS)> Filters);

public readonly record struct SubscribeMessage5(ChannelWriter<Message5> QueueWriter, IEnumerable<(byte[] Topic, byte QoS)> Filters);

public readonly record struct UnsubscribeMessage(IEnumerable<byte[]> Filters);

public enum ConnectionStatus
{
    Connected,
    Disconnected
}

public readonly record struct ConnectionStateChangedMessage(ConnectionStatus Status, string ClientId);