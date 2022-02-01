namespace System.Net.Mqtt.Client;

public readonly record struct MqttMessage(string Topic, ReadOnlyMemory<byte> Payload, bool Retained);