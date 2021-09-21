namespace System.Net.Mqtt.Client;

public record MqttMessage(string Topic, Memory<byte> Payload, bool Retained);