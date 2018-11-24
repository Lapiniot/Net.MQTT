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
        protected override Task OnSubscribeAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b10000010 || !SubscribePacket.TryParsePayload(buffer, out var packet))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "SUBSCRIBE"));
            }

            var result = state.Subscribe(packet.Topics);

            var subAckPacket = new SubAckPacket(packet.Id, result);

            return SendPacketAsync(subAckPacket, cancellationToken);
        }

        protected override async Task OnUnsubscribeAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b10100010 || !UnsubscribePacket.TryParsePayload(buffer, out var packet))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "UNSUBSCRIBE"));
            }

            state.Unsubscribe(packet.Topics);

            var id = packet.Id;

            await SendPacketAsync(new byte[] {(byte)PacketType.UnsubAck, 2, (byte)(id >> 8), (byte)id}, cancellationToken).ConfigureAwait(false);
        }
    }
}