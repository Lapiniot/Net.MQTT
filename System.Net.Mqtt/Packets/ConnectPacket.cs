using System.Buffers;
using System.Net.Mqtt.Extensions;
using static System.String;
using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets;

public class ConnectPacket : MqttPacket
{
    public ConnectPacket(string clientId, byte protocolLevel, string protocolName,
        ushort keepAlive = 120, bool cleanSession = true, string userName = null, string password = null,
        string willTopic = null, Memory<byte> willMessage = default, byte willQoS = 0x00, bool willRetain = false)
    {
        ClientId = clientId;
        ProtocolLevel = protocolLevel;
        ProtocolName = protocolName;
        KeepAlive = keepAlive;
        CleanSession = cleanSession;
        UserName = userName;
        Password = password;
        WillTopic = willTopic;
        WillMessage = willMessage;
        WillQoS = willQoS;
        WillRetain = willRetain;
    }

    public ushort KeepAlive { get; }
    public string UserName { get; }
    public string Password { get; }
    public string ClientId { get; }
    public string WillTopic { get; }
    public Memory<byte> WillMessage { get; }
    public byte WillQoS { get; }
    public bool WillRetain { get; }
    public bool CleanSession { get; }
    public string ProtocolName { get; }
    public byte ProtocolLevel { get; }

    protected internal int GetPayloadSize()
    {
        var willMessageLength = 2 + WillMessage.Length;

        return (IsNullOrEmpty(ClientId) ? 2 : 2 + UTF8.GetByteCount(ClientId)) +
               (IsNullOrEmpty(UserName) ? 0 : 2 + UTF8.GetByteCount(UserName)) +
               (IsNullOrEmpty(Password) ? 0 : 2 + UTF8.GetByteCount(Password)) +
               (IsNullOrEmpty(WillTopic) ? 0 : 2 + UTF8.GetByteCount(WillTopic) + willMessageLength);
    }

    protected internal int GetHeaderSize()
    {
        return 6 + UTF8.GetByteCount(ProtocolName);
    }

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out ConnectPacket packet, out int consumed)
    {
        if(TryRead(sequence.FirstSpan, out packet, out consumed))
        {
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if(TryReadMqttHeader(ref reader, out var header, out var size) && size <= reader.Remaining && header == ConnectMask)
        {
            if(!TryReadMqttString(ref reader, out var protocol) || !reader.TryRead(out var level) ||
               !reader.TryRead(out var connFlags) || !reader.TryReadBigEndian(out short keepAlive) ||
               !TryReadMqttString(ref reader, out var clientId))
            {
                reader.Rewind(remaining - reader.Remaining);
                return false;
            }

            string topic = null;
            byte[] willMessage = null;
            if((connFlags & WillMask) == WillMask)
            {
                if(!TryReadMqttString(ref reader, out topic) || !reader.TryReadBigEndian(out short value))
                {
                    reader.Rewind(remaining - reader.Remaining);
                    return false;
                }

                var willSize = (ushort)value;
                if(willSize > 0)
                {
                    willMessage = new byte[willSize];
                    if(!reader.TryCopyTo(willMessage)) return false;
                    reader.Advance(willSize);
                }
            }

            string userName = null;
            string password = null;
            if((connFlags & UserNameMask) == UserNameMask && !TryReadMqttString(ref reader, out userName) ||
               (connFlags & PasswordMask) == PasswordMask && !TryReadMqttString(ref reader, out password))
            {
                reader.Rewind(remaining - reader.Remaining);
                return false;
            }

            packet = new ConnectPacket(clientId, level, protocol, (ushort)keepAlive,
                (connFlags & CleanSessionMask) == CleanSessionMask, userName, password, topic, willMessage,
                (byte)((connFlags >> 3) & QoSMask), (connFlags & WillRetainMask) == WillRetainMask);

            return true;
        }

        reader.Advance(remaining - reader.Remaining);

        return false;
    }

    private static bool TryRead(in ReadOnlySpan<byte> span, out ConnectPacket packet, out int consumed)
    {
        packet = null;
        consumed = 0;

        if(TryReadMqttHeader(in span, out var header, out var size, out var offset) &&
           offset + size <= span.Length && header == ConnectMask)
        {
            var current = span.Slice(offset, size);

            if(!TryReadUInt16BigEndian(current, out var len) || current.Length < len + 8) return false;

            var protocol = UTF8.GetString(current.Slice(2, len));
            current = current[(len + 2)..];

            var level = current[0];
            var connFlags = current[1];
            current = current[2..];

            var keepAlive = ReadUInt16BigEndian(current);
            current = current[2..];

            len = ReadUInt16BigEndian(current);
            string clientId = null;
            if(len > 0)
            {
                if(current.Length < len + 2) return false;
                clientId = UTF8.GetString(current.Slice(2, len));
            }

            current = current[(len + 2)..];

            string willTopic = null;
            byte[] willMessage = default;
            if((connFlags & WillMask) == WillMask)
            {
                if(!TryReadUInt16BigEndian(current, out len) || len == 0 || current.Length < len + 2) return false;
                willTopic = UTF8.GetString(current.Slice(2, len));
                current = current[(len + 2)..];

                if(!TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                if(len > 0)
                {
                    willMessage = new byte[len];
                    current.Slice(2, len).CopyTo(willMessage);
                }

                current = current[(len + 2)..];
            }

            string userName = null;
            if((connFlags & UserNameMask) == UserNameMask)
            {
                if(!TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                userName = UTF8.GetString(current.Slice(2, len));
                current = current[(len + 2)..];
            }

            string password = null;
            if((connFlags & PasswordMask) == PasswordMask)
            {
                if(!TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                password = UTF8.GetString(current.Slice(2, len));
            }

            packet = new ConnectPacket(clientId, level, protocol, keepAlive,
                (connFlags & CleanSessionMask) == CleanSessionMask, userName, password, willTopic, willMessage,
                (byte)((connFlags >> 3) & QoSMask), (connFlags & WillRetainMask) == WillRetainMask);

            return true;
        }

        consumed = 0;
        return false;
    }

    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        remainingLength = GetHeaderSize() + GetPayloadSize();
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        var hasClientId = !IsNullOrEmpty(ClientId);
        var hasUserName = !IsNullOrEmpty(UserName);
        var hasPassword = !IsNullOrEmpty(Password);
        var hasWillTopic = !IsNullOrEmpty(WillTopic);

        // Packet flags
        span[0] = ConnectMask;
        span = span[1..];
        // Remaining length bytes
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        // Protocol info bytes
        span = span[WriteMqttString(ref span, ProtocolName)..];
        span[0] = ProtocolLevel;
        // Connection flag
        var flags = (byte)(WillQoS << 3);
        if(hasUserName) flags |= UserNameMask;
        if(hasPassword) flags |= PasswordMask;
        if(WillRetain) flags |= WillRetainMask;
        if(hasWillTopic) flags |= WillMask;
        if(CleanSession) flags |= CleanSessionMask;
        span[1] = flags;
        span = span[2..];
        // KeepAlive bytes
        WriteUInt16BigEndian(span, KeepAlive);
        span = span[2..];
        // Payload bytes
        if(hasClientId)
        {
            span = span[WriteMqttString(ref span, ClientId)..];
        }
        else
        {
            span[0] = 0;
            span[1] = 0;
            span = span[2..];
        }

        // Last will
        if(hasWillTopic)
        {
            span = span[WriteMqttString(ref span, WillTopic)..];
            var messageSpan = WillMessage.Span;
            var spanLength = messageSpan.Length;
            WriteUInt16BigEndian(span, (ushort)spanLength);
            span = span[2..];
            messageSpan.CopyTo(span);
            span = span[spanLength..];
        }

        // Username
        if(hasUserName)
        {
            span = span[WriteMqttString(ref span, UserName)..];
        }

        //Password
        if(hasPassword)
        {
            WriteMqttString(ref span, Password);
        }
    }

    #endregion
}