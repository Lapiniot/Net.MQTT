using System.Runtime.InteropServices;
using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession3
{
    [Union]
    [StructLayout(LayoutKind.Explicit)]
    private readonly record struct PacketDescriptor : PacketDescriptor.IUnionMembers
    {
        [FieldOffset(0x00)] private readonly IMqttPacket? packet;
        [FieldOffset(0x00)] private readonly ReadOnlyMemory<byte> topic;
        [FieldOffset(0x10)] private readonly ReadOnlyMemory<byte> payload;
        [FieldOffset(0x20)] private readonly uint raw;

        private PacketDescriptor(uint raw) => this.raw = raw;

        private PacketDescriptor(IMqttPacket packet, PacketType type)
        {
            this.packet = packet;
            raw = (uint)type;
        }

        private PacketDescriptor(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, uint flags)
        {
            this.topic = topic;
            this.payload = payload;
            raw = flags;
        }

        public object? Value => topic.Length is not 0
            ? topic
            : packet is not null
                ? packet
                : (raw & 0xF000_0000) is not 0
                    ? raw
                    : (raw & 0xF000) is not 0
                        ? (ushort)raw
                        : null;

        public bool HasValue => topic.Length is not 0 || packet is not null || (raw & 0xF000_F000) is not 0;

        public byte Flags => (byte)raw;
        public ushort Id => (ushort)(raw >>> 8);
        public ReadOnlyMemory<byte> Payload => payload;
        public uint Tag => raw;

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
            if (topic.Length is 0 && packet is null && (raw & 0xF000) is not 0)
            {
                value = (ushort)raw;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetValue([NotNullWhen(true)] out IMqttPacket? value)
        {
            if (topic.Length is 0 && packet is not null)
            {
                value = packet;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetValue(out ReadOnlyMemory<byte> value)
        {
            if (topic.Length is not 0)
            {
                value = topic;
                return true;
            }

            value = default;
            return false;
        }

        public interface IUnionMembers
        {
            static PacketDescriptor Create(uint value) => new(value);
            static PacketDescriptor Create(ushort value) => new(value);
            static PacketDescriptor Create(SubAckPacket value) => new(value, PacketType.SUBACK);
            static PacketDescriptor Create(ReadOnlyMemory<byte> value) => new(value, default, 0);
            static PacketDescriptor Create(ushort id, ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, byte flags) =>
                new(topic, payload, (uint)(flags | (id << 8)));

            object? Value { get; }
            bool HasValue { get; }

            bool TryGetValue(out uint value);
            bool TryGetValue(out ushort value);
            bool TryGetValue(out IMqttPacket? value);
            bool TryGetValue(out ReadOnlyMemory<byte> value);
        }
    }
}