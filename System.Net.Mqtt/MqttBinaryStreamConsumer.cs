using SequenceExtensions = System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt;

public abstract class MqttBinaryStreamConsumer : PipeConsumer
{
    protected MqttBinaryStreamConsumer(PipeReader reader) : base(reader) { }

    protected internal abstract void OnPacketReceived(byte packetType, int totalLength);

    protected sealed override bool Consume(ref ReadOnlySequence<byte> buffer)
    {
        if (SequenceExtensions.TryReadMqttHeader(in buffer, out var flags, out var length, out var offset))
        {
            var total = offset + length;
            if (total > buffer.Length) return false;
            var type = (byte)(flags >> 4);
            var reminder = buffer.Slice(offset, length);
            Dispatch((PacketType)type, flags, reminder);
            OnPacketReceived(type, total);
            buffer = buffer.Slice(total);
            return true;
        }

        if (buffer.Length >= 5)
        {
            MqttPacketHelpers.ThrowInvalidData();
        }

        return false;
    }

    protected abstract void Dispatch(PacketType type, byte flags, in ReadOnlySequence<byte> reminder);
}