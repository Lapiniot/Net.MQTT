namespace System.Net.Mqtt.Server;

public readonly record struct Message(Utf8String Topic, ReadOnlyMemory<byte> Payload, byte QoSLevel, bool Retain);

public readonly record struct IncomingMessage(in Message Message, string ClientId);

public readonly record struct SubscriptionRequest(MqttServerSessionState State, IEnumerable<(Utf8String topic, byte qosLevel)> Filters);