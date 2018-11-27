﻿using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class ServerSession
    {
        protected override void OnSubscribe(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b10000010 || !SubscribePacket.TryParsePayload(buffer, out var packet))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "SUBSCRIBE"));
            }

            var result = state.Subscribe(packet.Topics);

            Post(new SubAckPacket(packet.Id, result));

            Server.OnSubscribe(state, packet.Topics);
        }

        protected override void OnUnsubscribe(byte header, ReadOnlySequence<byte> buffer)
        {
            if(header != 0b10100010 || !UnsubscribePacket.TryParsePayload(buffer, out var packet))
            {
                throw new InvalidDataException(string.Format(Strings.InvalidPacketTemplate, "UNSUBSCRIBE"));
            }

            state.Unsubscribe(packet.Topics);

            var id = packet.Id;

            Post(new byte[] {(byte)PacketType.UnsubAck, 2, (byte)(id >> 8), (byte)id});
        }
    }
}