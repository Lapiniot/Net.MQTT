namespace System.Net.Mqtt.Server;

public readonly record struct Message(string Topic, in Memory<byte> Payload, byte QoSLevel, bool Retain);

public readonly record struct MessageRequest(in Message Message, string ClientId);

public readonly record struct SubscriptionRequest(MqttServerSessionState State, IEnumerable<(string topic, byte qosLevel)> Filters);