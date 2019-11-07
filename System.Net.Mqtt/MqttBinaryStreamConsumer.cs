﻿using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Mqtt.Extensions;
using System.Net.Pipes;
using static System.Net.Mqtt.Properties.Strings;

namespace System.Net.Mqtt
{
    public abstract class MqttBinaryStreamConsumer : PipeConsumer
    {
        private readonly MqttPacketHandler[] handlers;

        protected MqttBinaryStreamConsumer(PipeReader reader) : base(reader)
        {
            handlers = new MqttPacketHandler[16];
        }

        protected void SetHandler(int index, MqttPacketHandler handler)
        {
            if(index < 0 || index > 15) throw new ArgumentException($"Invalid '{nameof(index)}'. Accepted values are in the range [0..15].");
            if(handler is null) throw new ArgumentNullException(nameof(handler));

            handlers[index] = handler;
        }

        protected override long Consume(in ReadOnlySequence<byte> buffer)
        {
            if(buffer.TryReadMqttHeader(out var flags, out var length, out var offset))
            {
                if(offset + length > buffer.Length) return 0;

                var handler = handlers[flags >> 4];

                if(handler == null) throw new InvalidDataException(UnexpectedPacketType);

                handler.Invoke(flags, buffer.Slice(offset, length));

                OnPacketReceived();

                return offset + length;
            }

            if(buffer.Length >= 5) throw new InvalidDataException(InvalidDataStream);

            return 0;
        }

        protected abstract void OnPacketReceived();
    }
}