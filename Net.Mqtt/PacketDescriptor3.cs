namespace Net.Mqtt;

[Union]
[StructLayout(LayoutKind.Explicit)]
internal readonly struct PacketDescriptor3 : PacketDescriptor3.IUnionMembers
{
    [FieldOffset(0x00)] private readonly IMqttPacketWriter? packet;
    [FieldOffset(0x00)] private readonly ReadOnlyMemory<byte> topic;
    [FieldOffset(0x10)] private readonly ReadOnlyMemory<byte> payload;
    [FieldOffset(0x20)] private readonly uint raw;

    private PacketDescriptor3(uint raw) => this.raw = raw;

    private PacketDescriptor3(ushort raw) => this.raw = raw;

    private PacketDescriptor3(IMqttPacketWriter packet) => this.packet = packet;

    private PacketDescriptor3(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, uint flags)
    {
        this.topic = topic;
        this.payload = payload;
        raw = flags;
    }

    public object? Value => topic.Length is not 0
        ? (topic, payload, raw)
        : packet is not null
            ? packet
            : (raw & 0xF000_0000) is not 0
                ? raw
                : (raw & 0xFFFF_0000) is 0 && (raw & 0xF000) is not 0
                    ? (ushort)raw
                    : null;

    public bool HasValue => topic.Length is not 0
        || packet is not null
        || (raw & 0xF000_0000) is not 0
        || (raw & 0xFFFF_0000) is 0 && (raw & 0xF000) is not 0;

    public bool TryGetValue(out uint value)
    {
        if (topic.Length is 0 && packet is null && (raw & 0xF000_0000) is not 0)
        {
            value = raw;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValue(out ushort value)
    {
        if (topic.Length is 0 && packet is null && (raw & 0xFFFF_0000) is 0 && (raw & 0xF000) is not 0)
        {
            value = (ushort)raw;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValue([NotNullWhen(true)] out IMqttPacketWriter? value)
    {
        if (topic.Length is 0 && packet is not null)
        {
            value = packet;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValue(out PublishBlock value)
    {
        if (topic.Length is not 0)
        {
            value = (topic, payload, raw);
            return true;
        }

        value = default;
        return false;
    }

    public interface IUnionMembers
    {
        static PacketDescriptor3 Create(uint value) => new(value);
        static PacketDescriptor3 Create(ushort value) => new(value);
        static PacketDescriptor3 Create(IMqttPacketWriter value) => new(value);
        static PacketDescriptor3 Create(PublishBlock value)
        {
            ArgumentOutOfRangeException.ThrowIfZero(value.Topic.Length, nameof(value.Topic));
            return new(value.Topic, value.Payload, value.Flags);
        }

        object? Value { get; }
        bool HasValue { get; }

        bool TryGetValue(out uint value);
        bool TryGetValue(out ushort value);
        bool TryGetValue(out IMqttPacketWriter? value);
        bool TryGetValue(out PublishBlock value);
    }
}