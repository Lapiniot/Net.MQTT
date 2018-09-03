using System.Buffers;
using System.Net.Mqtt.Messages;
using System.Net.Sockets;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.QoSLevel;
using static System.Net.Sockets.SocketFlags;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        #region Overrides of NetworkStreamParser<MqttConnectionOptions>

        protected override void ParseBuffer(in ReadOnlySequence<byte> buffer, out int consumed)
        {
            consumed = 0;

            if(TryParseHeader(buffer, out var header, out var length))
            {
                var total = GetLengthByteCount(length) + 1 + length;

                if(total <= buffer.Length)
                {
                    var packetType = (PacketType)(header & TypeMask);

                    switch(packetType)
                    {
                        case PacketType.Publish:
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
                                    var pubRecMessage = new PubRecMessage(packetId);
                                    pubRecMap.TryAdd(packetId, pubRecMessage);
                                    Socket.SendAsync(new PubRelMessage(packetId).GetBytes(), None, default);
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
                                // if(TryReadUInt16(buffer.Slice(2), out var packetId) && subMap.TryGetValue(packetId,out var tcs))
                                // {
                                //     tcs.TrySetResult()
                                //     pubRecMap.TryRemove(packetId, out _);
                                //     idPool.Return(packetId);
                                // }

                                break;
                            }
                        case PacketType.UnsubAck:
                            break;
                        case PacketType.PingResp:
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