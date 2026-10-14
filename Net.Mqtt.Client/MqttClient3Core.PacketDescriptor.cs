using System.Runtime.InteropServices;
using Net.Mqtt.Packets.V3;

namespace Net.Mqtt.Client;

public partial class MqttClient3Core
{
    [Union]
    [StructLayout(LayoutKind.Explicit)]
    internal readonly record struct PacketDescriptor : PacketDescriptor.IUnionMembers
    {
        [FieldOffset(0x00)] private readonly IMqttPacket? packet;
        [FieldOffset(0x00)] private readonly ReadOnlyMemory<byte> topic;
        [FieldOffset(0x10)] private readonly ReadOnlyMemory<byte> payload;
        [FieldOffset(0x20)] private readonly uint raw;

        private PacketDescriptor(uint raw) => this.raw = raw;

        private PacketDescriptor(IMqttPacket packet) => this.packet = packet;

        private PacketDescriptor(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> payload, uint flags)
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

        public ReadOnlyMemory<byte> Payload => payload;

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

#pragma warning disable CA1034 // Nested types should not be visible
        public interface IUnionMembers
#pragma warning restore CA1034 // Nested types should not be visible
        {
            static PacketDescriptor Create(uint value) => new(value);
            static PacketDescriptor Create(ushort value) => new(value);
            static PacketDescriptor Create(ConnectPacket value) => new(value);
            static PacketDescriptor Create(SubscribePacket value) => new(value);
            static PacketDescriptor Create(UnsubscribePacket value) => new(value);
            static PacketDescriptor Create(PublishBlock value) => new(value.Topic, value.Payload, value.Flags);

            object? Value { get; }
            bool HasValue { get; }

            bool TryGetValue(out uint value);
            bool TryGetValue(out ushort value);
            bool TryGetValue(out IMqttPacket? value);
            bool TryGetValue(out PublishBlock value);
        }
    }
}