using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Server.Protocol.V5;

public partial class MqttServerSession5
{
    [Union]
    private readonly struct PacketDescriptor : PacketDescriptor.IUnionMembers
    {
        private readonly IMqttPacket5? packet;
        private readonly uint raw;

        private PacketDescriptor(uint raw) : this() => this.raw = raw;

        private PacketDescriptor(IMqttPacket5? packet, PacketType type) : this()
        {
            this.packet = packet;
            raw = (uint)type;
        }

        public bool HasValue => packet is not null || (raw & 0xF000_F000) is not 0;

        public object? Value => packet is not null
            ? packet
            : (raw & 0xF000_0000) is not 0
                ? raw
                : (raw & 0xF000) is not 0
                    ? (ushort)raw
                    : null;

        public uint Tag => raw;

        public bool TryGetValue([NotNullWhen(true)] out IMqttPacket5? value)
        {
            if (packet is not null)
            {
                value = packet;
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
            if (packet is null && (raw & 0xF000) is not 0)
            {
                value = (ushort)raw;
                return true;
            }

            value = default;
            return false;
        }

        public interface IUnionMembers
        {
            static PacketDescriptor Create(uint value) => new(value);
            static PacketDescriptor Create(ushort value) => new(value);
            static PacketDescriptor Create(PublishPacket value) => new(value, PacketType.PUBLISH);
            static PacketDescriptor Create(PubAckPacket value) => new(value, PacketType.PUBACK);
            static PacketDescriptor Create(PubRecPacket value) => new(value, PacketType.PUBREC);
            static PacketDescriptor Create(PubRelPacket value) => new(value, PacketType.PUBREL);
            static PacketDescriptor Create(PubCompPacket value) => new(value, PacketType.PUBCOMP);
            static PacketDescriptor Create(SubAckPacket value) => new(value, PacketType.SUBACK);
            static PacketDescriptor Create(UnsubAckPacket value) => new(value, PacketType.UNSUBACK);

            bool HasValue { get; }
            object? Value { get; }

            bool TryGetValue(out IMqttPacket5? value);
            bool TryGetValue(out PublishPacket? value);
            bool TryGetValue(out uint value);
            bool TryGetValue(out ushort value);
        }
    }
}