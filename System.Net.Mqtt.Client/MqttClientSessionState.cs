namespace System.Net.Mqtt.Client;

public class MqttClientSessionState : MqttSessionState<PublishDeliveryState>
{
    private readonly AsyncCountdownEvent inFightCounter;
    private int completed;
    private volatile Task completedTask;

    public MqttClientSessionState(int maxInflight) : base(maxInflight) =>
        inFightCounter = new(1 /*Initialize with 1 preventing ACE to become immediately signaled*/);

    public Task CompleteAsync()
    {
        if (Interlocked.CompareExchange(ref completed, 1, 0) is 0)
        {
            inFightCounter.Signal(); // Add signal in order to compensate one "dummy" initialCount signal upon ACE init
            completedTask = inFightCounter.WaitAsync();
        }
        else
        {
            var spinWait = new SpinWait();
            while (completedTask is null)
            {
                spinWait.SpinOnce(-1);
            }
        }

        return completedTask;
    }

    #region Overrides of MqttSessionState

    /// <inheritdoc />
    public async Task<ushort> CreateMessageDeliveryStateAsync(byte flags, ReadOnlyMemory<byte> topic,
        ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var id = await CreateMessageDeliveryStateAsync(new((byte)(flags | PacketFlags.Duplicate), topic, payload),
            cancellationToken).ConfigureAwait(false);
        inFightCounter.AddCount();
        return id;
    }

    /// <inheritdoc />
    public new bool DiscardMessageDeliveryState(ushort packetId)
    {
        if (!base.DiscardMessageDeliveryState(packetId)) return false;
        inFightCounter.Signal();
        return true;
    }

    #endregion
}