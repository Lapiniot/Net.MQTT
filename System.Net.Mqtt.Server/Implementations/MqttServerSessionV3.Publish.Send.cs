using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3
    {
        private async Task DispatchMessageAsync(object arg, CancellationToken cancellationToken)
        {
            var (topic, payload, qoSLevel, _) = await state.DequeueAsync(cancellationToken).ConfigureAwait(false);

            switch(qoSLevel)
            {
                case AtMostOnce:
                {
                    var publishPacket = new PublishPacket(0, default, topic, payload);
                    await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                    break;
                }
                case AtLeastOnce:
                case ExactlyOnce:
                {
                    var publishPacket = state.AddResendPacket(id => new PublishPacket(id, qoSLevel, topic, payload));
                    await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(qoSLevel), qoSLevel, null);
            }
        }

        protected override Task OnPubAckAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0100_0000 || !TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBACK"));
            }

            state.RemoveResendPacket(id);

            return Task.CompletedTask;
        }

        protected override async Task OnPubRecAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0101_0000 || !TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBREC"));
            }

            var pubRelPacket = new PubRelPacket(id);

            state.UpdateResendPacket(id, pubRelPacket);

            await SendPublishResponseAsync(PubRel, id, cancellationToken).ConfigureAwait(false);
        }

        protected override Task OnPubCompAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0111_0000 || !TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBCOMP"));
            }

            state.RemoveResendPacket(id);

            return Task.CompletedTask;
        }
    }
}