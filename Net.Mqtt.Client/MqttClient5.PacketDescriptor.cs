using Net.Mqtt.Packets.V5;

namespace Net.Mqtt.Client;

public partial class MqttClient5
{
    [Union]
    private readonly struct PacketDescriptor : PacketDescriptor.IUnionMembers
    {
        private readonly IMqttPacket5? packet;
        private readonly uint raw;

        private PacketDescriptor(uint raw) => this.raw = raw;

        private PacketDescriptor(IMqttPacket5 packet) => this.packet = packet;

        public bool HasValue => packet is not null || (raw & 0xF000_F000) is not 0;

        public object? Value => packet is not null
            ? packet
            : (raw & 0xF000_0000) is not 0
                ? raw
                : (raw & 0xF000) is not 0
                    ? (ushort)raw
                    : null;

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
            static PacketDescriptor Create(ConnectPacket value) => new(value);
            static PacketDescriptor Create(PublishPacket value) => new(value);
            static PacketDescriptor Create(PubRelPacket value) => new(value);
            static PacketDescriptor Create(PubCompPacket value) => new(value);
            static PacketDescriptor Create(SubscribePacket value) => new(value);
            static PacketDescriptor Create(UnsubscribePacket value) => new(value);
            static PacketDescriptor Create(uint value) => new(value);
            static PacketDescriptor Create(ushort value) => new(value);

            bool HasValue { get; }
            object? Value { get; }

            bool TryGetValue(out IMqttPacket5? value);
            bool TryGetValue(out PublishPacket? value);
            bool TryGetValue(out uint value);
            bool TryGetValue(out ushort value);
        }
    }
}