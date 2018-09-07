using System.Buffers;
using System.Diagnostics;
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
                            if(PublishPacket.TryParse(buffer,out var p)){
                                
                            }
                            break;
                        case PacketType.PubAck:
                            {
                                if(TryReadUInt16(buffer.Slice(2), out var packetId))
                                {
                                    pubMap.TryRemove(packetId, out _);
                                    idPool.Return(packetId);
                                }

                                break;
                            }
                        case PacketType.PubRec:
                            {
                                if(TryReadUInt16(buffer.Slice(2), out var packetId))
                                {
                                    pubMap.TryRemove(packetId, out _);
                                    var pubRecMessage = new PubRecPacket(packetId);
                                    pubRecMap.TryAdd(packetId, pubRecMessage);
                                    var unused = MqttSendPacketAsync(new PubRelPacket(packetId));
                                }

                                break;
                            }
                        case PacketType.PubComp:
                            {
                                if(TryReadUInt16(buffer.Slice(2), out var packetId))
                                {
                                    pubRecMap.TryRemove(packetId, out _);
                                    idPool.Return(packetId);
                                }

                                break;
                            }
                        case PacketType.SubAck:
                            {
                                if(TryReadUInt16(buffer.Slice(offset), out var packetId))
                                {
                                    var result = buffer.Slice(offset + 2, length - 2).ToArray();
                                    AcknowledgeSubscription(packetId, result);
                                }

                                break;
                            }
                        case PacketType.UnsubAck:
                            {
                                if(TryReadUInt16(buffer.Slice(offset), out var packetId))
                                {
                                    AcknowledgeUnsubscription(packetId);
                                }

                                break;
                            }
                        case PacketType.PingResp:
                            Trace.WriteLine(DateTime.Now.TimeOfDay + ": Ping response from server");
                            break;
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