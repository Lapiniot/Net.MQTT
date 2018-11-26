using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketType;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;
using static System.Threading.Tasks.Task;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession
    {
        private async Task ProcessMessageAsync(object arg, CancellationToken cancellationToken)
        {
            var (topic, payload, qoSLevel, _) = await state.DequeueAsync(cancellationToken).ConfigureAwait(false);

            switch(qoSLevel)
            {
                case 0:
                {
                    var publishPacket = new PublishPacket(0, default, topic, payload);
                    Post(publishPacket);
                    break;
                }
                case 1:
                case 2:
                {
                    var publishPacket = state.AddPublishToResend(topic, payload, qoSLevel);
                    Post(publishPacket);
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

            state.RemoveFromResend(id);

            return CompletedTask;
        }

        protected override Task OnPubRecAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0101_0000 || !TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBREC"));
            }

            state.AddPubRelToResend(id);

            PostPublishResponse(PubRel, id);

            return CompletedTask;
        }

        protected override Task OnPubCompAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0111_0000 || !TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketTemplate, "PUBCOMP"));
            }

            state.RemoveFromResend(id);

            return CompletedTask;
        }
    }
}