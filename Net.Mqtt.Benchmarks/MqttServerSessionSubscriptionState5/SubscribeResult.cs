using System.Collections.Immutable;

namespace Net.Mqtt.Benchmarks.MqttServerSessionSubscriptionState5;

public record SubscribeResult(ImmutableArray<byte> Feedback, IReadOnlyList<(byte[] Filter, bool Exists, SubscriptionOptions Options)> Subscriptions, int TotalCount);