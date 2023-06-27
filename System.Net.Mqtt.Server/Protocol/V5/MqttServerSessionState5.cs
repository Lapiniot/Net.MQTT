namespace System.Net.Mqtt.Server.Protocol.V5;

public sealed class MqttServerSessionState5 : MqttServerSessionState<Message5, Message5, MqttServerSessionSubscriptionState5>, IDisposable
{
    private WillMessageState WillState;
    private int published;

    public MqttServerSessionState5(string clientId, DateTime createdAt, int maxInFlight) :
        base(clientId, new MqttServerSessionSubscriptionState5(), Channel.CreateUnbounded<Message5>(), createdAt, maxInFlight)
    { }

    public bool TopicMatches(ReadOnlySpan<byte> topic, [NotNullWhen(true)] out SubscriptionOptions? options, out IReadOnlyList<uint>? subscriptionIds) =>
        Subscriptions.TopicMatches(topic, out options, out subscriptionIds);

    public Task<ushort> CreateMessageDeliveryStateAsync(Message5 message, CancellationToken cancellationToken)
        => CreateDeliveryStateCoreAsync(message, cancellationToken);

    public void DiscardMessageDeliveryState(ushort id) => DiscardDeliveryStateCore(id);

    public void SetWillMessageState(Message5? willMessage, IObserver<IncomingMessage5> incomingObserver)
    {
        WillState.Timer?.Dispose();
        WillState = new(willMessage, incomingObserver, null);
        Volatile.Write(ref published, 0);
    }

    public void DiscardWillMessageState()
    {
        Volatile.Write(ref published, 1);
        WillState.Timer?.Dispose();
        WillState = default;
    }

    public void PublishWillMessage(TimeSpan delay)
    {
        if (WillState is { Message: { } message, Observer: { } observer })
        {
            if (delay.TotalSeconds < 1)
                PublishOnce(message, observer);
            else
                PublishOnceDelayedAsync(message, observer, delay).Observe();
        }

        async Task PublishOnceDelayedAsync(Message5 message, IObserver<IncomingMessage5> observer, TimeSpan delay)
        {
            using var timer = new PeriodicTimer(delay);
            WillState = WillState with { Timer = timer };
            if (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                PublishOnce(message, observer);
            }
        }
    }

    private void PublishOnce(Message5 message, IObserver<IncomingMessage5> observer)
    {
        if (Interlocked.Exchange(ref published, 1) == 0)
        {
            observer.OnNext(new(message, this));
            WillState = default;
        }
    }

    public void Dispose()
    {
        if (WillState is { Message: { } message, Observer: { } observer, Timer: var timer })
        {
            timer?.Dispose();
            PublishOnce(message, observer);
        }
    }

    private record struct WillMessageState(Message5? Message, IObserver<IncomingMessage5>? Observer, PeriodicTimer? Timer);
}