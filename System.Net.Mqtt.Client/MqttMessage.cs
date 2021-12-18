namespace System.Net.Mqtt.Client;

public record MqttMessage(string Topic, ReadOnlyMemory<byte> Payload, bool Retained);