using SequenceExtensions = Net.Mqtt.Extensions.SequenceExtensions;

namespace Net.Mqtt;

public abstract class MqttBinaryStreamConsumer(PipeReader reader) : PipeConsumer(reader)
{
    private int maxPacketSize = int.MaxValue;

    public int MaxReceivePacketSize
    {
        get => maxPacketSize;
        protected set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            maxPacketSize = value;
        }
    }

    protected sealed override void Consume(ref ReadOnlySequence<byte> buffer)
    {
        while (SequenceExtensions.TryReadMqttHeader(in buffer, out var header,
            out var remainingLength, out var fixedHeaderLength))
        {
            var total = fixedHeaderLength + remainingLength;
            if (total > maxPacketSize)
            {
                PacketTooLargeException.Throw();
            }

            if (total > buffer.Length)
            {
                return;
            }

            var reminder = buffer.Slice(fixedHeaderLength, remainingLength);
            Dispatch(header, total, reminder);
            buffer = buffer.Slice(total);
        }

        if (buffer.Length >= 5)
        {
            MalformedPacketException.Throw();
        }

        return;
    }

    protected abstract void Dispatch(byte header, int total, in ReadOnlySequence<byte> reminder);
}