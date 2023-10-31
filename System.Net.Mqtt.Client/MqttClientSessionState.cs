namespace System.Net.Mqtt.Client;

public class MqttClientSessionState : MqttSessionState<PublishDeliveryState>
{
    private readonly AsyncCountdownEvent inFlightCounter;
    private int completed;
    private volatile Task completedTask;

    public MqttClientSessionState() : base() => inFlightCounter = new(1 /*Initialize with 1 preventing ACE to become immediately signaled*/);

    public Task CompleteAsync()
    {
        if (Interlocked.CompareExchange(ref completed, 1, 0) is 0)
        {
            inFlightCounter.Signal(); // Add signal in order to compensate one "dummy" initialCount signal upon ACE init
            completedTask = inFlightCounter.WaitAsync(default);
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

    public ushort CreateMessageDeliveryState(byte flags, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload)
    {
        var id = CreateDeliveryStateCore(new((byte)(flags | PacketFlags.Duplicate), topic, payload));
        inFlightCounter.AddCount();
        return id;
    }

    public bool DiscardMessageDeliveryState(ushort packetId)
    {
        if (!DiscardDeliveryStateCore(packetId)) return false;
        inFlightCounter.Signal();
        return true;
    }
}