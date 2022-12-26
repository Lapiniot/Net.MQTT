namespace System.Net.Mqtt;

public abstract class MqttBinaryStreamConsumer : PipeConsumer
{
    private readonly MqttPacketHandler[] handlers;

    protected MqttBinaryStreamConsumer(PipeReader reader) : base(reader) =>
        handlers = new MqttPacketHandler[16];

    protected MqttPacketHandler this[PacketType index]
    {
        get => handlers[(int)index];
        set => handlers[(int)index] = value;
    }

    protected internal abstract void OnPacketReceived(byte packetType, int totalLength);

    protected sealed override bool Consume(ref ReadOnlySequence<byte> buffer)
    {
        if (SE.TryReadMqttHeader(in buffer, out var flags, out var length, out var offset))
        {
            var total = offset + length;
            if (total > buffer.Length) return false;
            var type = (byte)(flags >> 4);
            var handler = handlers[type];
            if (handler is not null)
                handler.Invoke(flags, buffer.Slice(offset, length));
            else
                MqttPacketHelpers.ThrowUnexpectedType(type);
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
}