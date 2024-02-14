namespace System.Net.Mqtt.Client;

public readonly record struct MqttMessage(ReadOnlyMemory<byte> Topic, ReadOnlyMemory<byte> Payload, bool Retained);