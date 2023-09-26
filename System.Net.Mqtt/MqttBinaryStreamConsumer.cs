using SequenceExtensions = System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt;

public abstract class MqttBinaryStreamConsumer : PipeConsumer
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

    protected MqttBinaryStreamConsumer(PipeReader reader) : base(reader) { }

    protected sealed override bool Consume(ref ReadOnlySequence<byte> buffer)
    {
        if (SequenceExtensions.TryReadMqttHeader(in buffer, out var header, out var length, out var offset))
        {
            var total = offset + length;
            if (total > maxPacketSize)
            {
                PacketTooLargeException.Throw();
            }

            if (total > buffer.Length)
            {
                return false;
            }

            var reminder = buffer.Slice(offset, length);
            Dispatch(header, total, reminder);
            buffer = buffer.Slice(total);
            return true;
        }

        if (buffer.Length >= 5)
        {
            MalformedPacketException.Throw();
        }

        return false;
    }

    protected abstract void Dispatch(byte header, int total, in ReadOnlySequence<byte> reminder);
}