using System.Buffers;
using System.Net.Pipes;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt
{
    public abstract class MqttBinaryStreamHandler : NetworkPipeProcessor
    {
        protected readonly MqttPacketHandler[] Handlers;
        protected MqttPacketHandler UnsupportedTypeHandler;

        protected MqttBinaryStreamHandler(NetworkPipeReader reader) : base(reader)
        {
            Handlers = new MqttPacketHandler[16];
            UnsupportedTypeHandler = SkipUnknownPacket;
        }

        protected override void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;
            if(TryReadByte(buffer, out var flags))
            {
                var handler = Handlers[flags >> 4] ?? UnsupportedTypeHandler;
                if(handler(buffer, out var total))
                {
                    consumed = total;
                }
            }
        }

        protected static bool SkipUnknownPacket(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            if(TryParseHeader(buffer, out _, out var length, out var offset))
            {
                var size = offset + length;
                if(size <= buffer.Length)
                {
                    consumed = size;
                    return true;
                }
            }

            consumed = 0;
            return false;
        }
    }
}