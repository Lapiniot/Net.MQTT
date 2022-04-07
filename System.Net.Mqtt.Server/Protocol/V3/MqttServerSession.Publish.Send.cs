﻿using System.Buffers;
using static System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V3;

public partial class MqttServerSession
{
    private async Task RunMessagePublisherAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // TODO: try peek message from the queue and remove only after succesfull send over network operation 
            var (topic, payload, qos, _) = await sessionState.DequeueMessageAsync(stoppingToken).ConfigureAwait(false);

            switch (qos)
            {
                case 0:
                    PostPublish(0, 0, topic, in payload);
                    break;

                case 1:
                case 2:
                    var flags = (byte)(qos << 1);
                    var id = await sessionState.CreateMessageDeliveryStateAsync(flags, topic, payload, stoppingToken).ConfigureAwait(false);
                    PostPublish(flags, id, topic, in payload);
                    break;

                default:
                    throw new InvalidDataException("Invalid QosLevel value");
            }
        }
    }

    protected sealed override void OnPubAck(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBACK");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }

    protected sealed override void OnPubRec(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBREC");
        }

        sessionState.SetMessagePublishAcknowledged(id);
        Post(PacketFlags.PubRelPacketMask | id);
    }

    protected sealed override void OnPubComp(byte header, ReadOnlySequence<byte> reminder)
    {
        if (!TryReadUInt16(in reminder, out var id))
        {
            ThrowInvalidPacketFormat("PUBCOMP");
        }

        sessionState.DiscardMessageDeliveryState(id);
    }
}