using System.Buffers;
using System.Net.Mqtt.Packets;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        #region Overrides of NetworkStreamParser<MqttConnectionOptions>

        protected override void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;

            if(TryParseHeader(buffer, out var header, out var length, out var offset))
            {
                var total = GetLengthByteCount(length) + 1 + length;

                if(total <= buffer.Length)
                {
                    var packetType = (PacketType)(header & TypeMask);

                    switch(packetType)
                    {
                        case PacketType.Publish:
                        {
                            if(PublishPacket.TryParse(buffer, out var p))
                            {
                                OnPublishPacket(p);
                            }

                            break;
                        }

                        case PacketType.PubRel:
                        {
                            if(TryReadUInt16(buffer.Slice(2), out var packetId))
                            {
                                OnPublishReleasePacket(packetId);
                            }

                            break;
                        }

                        case PacketType.PubAck:
                        {
                            if(TryReadUInt16(buffer.Slice(2), out var packetId))
                            {
                                OnPublishAcknowledgePacket(packetId);
                            }

                            break;
                        }

                        case PacketType.PubRec:
                        {
                            if(TryReadUInt16(buffer.Slice(2), out var packetId))
                            {
                                OnPublishReceivePacket(packetId);
                            }

                            break;
                        }

                        case PacketType.PubComp:
                        {
                            if(TryReadUInt16(buffer.Slice(2), out var packetId))
                            {
                                OnPublishCompletePacket(packetId);
                            }

                            break;
                        }

                        case PacketType.SubAck:
                        {
                            if(TryReadUInt16(buffer.Slice(offset), out var packetId))
                            {
                                var resultOffset = offset + 2;
                                var resultLength = length - 2;
                                var result = buffer.IsSingleSegment || buffer.First.Length >= length + offset
                                    ? buffer.First.Span.Slice(resultOffset, resultLength).ToArray()
                                    : buffer.Slice(resultOffset, resultLength).ToArray();
                                OnSubscribeAcknowledgePacket(packetId, result);
                            }

                            break;
                        }

                        case PacketType.UnsubAck:
                        {
                            if(TryReadUInt16(buffer.Slice(offset), out var packetId))
                            {
                                OnUnsubscribeAcknwledgePacket(packetId);
                            }

                            break;
                        }

                        case PacketType.PingResp:
                        {
                            OnPingResponsePacket();
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    consumed = total;
                }
            }
        }

        #endregion
    }
}