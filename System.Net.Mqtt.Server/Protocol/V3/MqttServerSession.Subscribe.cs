﻿using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Mqtt.Packets;
using static System.Globalization.CultureInfo;
using static System.Net.Mqtt.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public partial class MqttServerSession
    {
        protected override void OnSubscribe(byte header, ReadOnlySequence<byte> sequence)
        {
            if(header != 0b10000010 || !SubscribePacket.TryReadPayload(sequence, (int)sequence.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "SUBSCRIBE"));
            }

            var array = packet.Topics.ToArray();

            var result = sessionState.Subscribe(array);

            Post(new SubAckPacket(packet.Id, result));

            subscribeObserver?.OnNext(new SubscriptionRequest(sessionState, array));
        }

        protected override void OnUnsubscribe(byte header, ReadOnlySequence<byte> sequence)
        {
            if(header != 0b10100010 || !UnsubscribePacket.TryReadPayload(sequence, (int)sequence.Length, out var packet))
            {
                throw new InvalidDataException(Format(InvariantCulture, InvalidPacketFormat, "UNSUBSCRIBE"));
            }

            sessionState.Unsubscribe(packet.Topics.ToArray());

            Post(new UnsubAckPacket(packet.Id));
        }
    }
}