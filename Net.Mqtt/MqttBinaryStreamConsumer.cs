﻿using SequenceExtensions = Net.Mqtt.Extensions.SequenceExtensions;

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

    protected sealed override bool Consume(ref ReadOnlySequence<byte> buffer)
    {
        if (SequenceExtensions.TryReadMqttHeader(in buffer, out var header,
            out var remainingLength, out var fixedHeaderLength))
        {
            var total = fixedHeaderLength + remainingLength;
            if (total > maxPacketSize)
                PacketTooLargeException.Throw();

            if (total > buffer.Length)
                return false;

            var reminder = buffer.Slice(fixedHeaderLength, remainingLength);
            Dispatch(header, total, reminder);
            buffer = buffer.Slice(total);
            return true;
        }

        if (buffer.Length >= 5)
            MalformedPacketException.Throw();

        return false;
    }

    protected abstract void Dispatch(byte header, int total, in ReadOnlySequence<byte> reminder);
}