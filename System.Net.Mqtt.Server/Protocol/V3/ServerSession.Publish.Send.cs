using System.Buffers;
using System.IO;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

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
                    Post(new PublishPacket(0, default, topic, payload));
                    break;
                }

                case 1:
                case 2:
                {
                    Post(state.AddPublishToResend(topic, payload, qoSLevel));
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(qoSLevel), qoSLevel, null);
            }
        }

        protected override void OnPubAck(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b0100_0000 || !buffer.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBACK"));
            }

            state.RemoveFromResend(id);
        }

        protected override void OnPubRec(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b0101_0000 || !buffer.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBREC"));
            }

            state.AddPubRelToResend(id);
            Post(new PubRelPacket(id));
        }

        protected override void OnPubComp(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b0111_0000 || !buffer.TryReadUInt16(out var id))
            {
                throw new InvalidDataException(Format(InvalidPacketFormat, "PUBCOMP"));
            }

            state.RemoveFromResend(id);
        }
    }
}