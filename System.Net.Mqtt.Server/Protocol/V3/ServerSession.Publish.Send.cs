﻿using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Properties;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession
    {
        private async Task DispatchMessageAsync(object arg, CancellationToken cancellationToken)
        {
            var (topic, payload, qoSLevel, _) = await state.DequeueAsync(cancellationToken).ConfigureAwait(false);

            switch(qoSLevel)
            {
                case QoSLevel.AtMostOnce:
                {
                    var publishPacket = new PublishPacket(0, default, topic, payload);
                    await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                    break;
                }
                case QoSLevel.AtLeastOnce:
                case QoSLevel.ExactlyOnce:
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
            if(header != 0b0100_0000 || !MqttHelpers.TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "PUBACK"));
            }

            state.RemoveResendPacket(id);

            return Task.CompletedTask;
        }

        protected override async Task OnPubRecAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0101_0000 || !MqttHelpers.TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "PUBREC"));
            }

            var pubRelPacket = new PubRelPacket(id);

            state.UpdateResendPacket(id, pubRelPacket);

            await SendPublishResponseAsync(PacketType.PubRel, id, cancellationToken).ConfigureAwait(false);
        }

        protected override Task OnPubCompAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b0111_0000 || !MqttHelpers.TryReadUInt16(buffer, out var id))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "PUBCOMP"));
            }

            state.RemoveResendPacket(id);

            return Task.CompletedTask;
        }
    }
}