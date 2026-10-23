using Net.Mqtt.Packets.V5;

namespace Net.Mqtt;

[Union]
internal readonly struct PacketDescriptor5 : PacketDescriptor5.IUnionMembers
{
    private readonly IMqttPacketWriter5? packet;
    private readonly uint raw;

    private PacketDescriptor5(uint raw) => this.raw = raw;

    private PacketDescriptor5(ushort raw) => this.raw = raw;

    private PacketDescriptor5(IMqttPacketWriter5 packet) => this.packet = packet;

    public bool HasValue => packet is not null
        || (raw & 0xF000_0000) is not 0
        || (raw & 0xFFFF_0000) is 0 && (raw & 0xF000) is not 0;

    public object? Value => packet is not null
        ? packet
        : (raw & 0xF000_0000) is not 0
            ? raw
            : (raw & 0xFFFF_0000) is 0 && (raw & 0xF000) is not 0
                ? (ushort)raw
                : null;

    public bool TryGetValue(out uint value)
    {
        if (packet is null && (raw & 0xF000_0000) is not 0)
        {
            value = raw;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValue(out ushort value)
    {
        if (packet is null && (raw & 0xFFFF_0000) is 0 && (raw & 0xF000) is not 0)
        {
            value = (ushort)raw;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValue([NotNullWhen(true)] out PublishPacket? value)
    {
        if (packet is PublishPacket publishPacket)
        {
            value = publishPacket;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValue([NotNullWhen(true)] out IMqttPacketWriter5? value)
    {
        if (packet is not null)
        {
            value = packet;
            return true;
        }

        value = default;
        return false;
    }

    public interface IUnionMembers
    {
        static PacketDescriptor5 Create(uint value) => new(value);
        static PacketDescriptor5 Create(ushort value) => new(value);
        static PacketDescriptor5 Create(IMqttPacketWriter5 value) => new(value);
        static PacketDescriptor5 Create(PublishPacket value) => new(value);

        bool HasValue { get; }
        object? Value { get; }

        bool TryGetValue(out IMqttPacketWriter5? value);
        bool TryGetValue(out PublishPacket? value);
        bool TryGetValue(out uint value);
        bool TryGetValue(out ushort value);
    }
}