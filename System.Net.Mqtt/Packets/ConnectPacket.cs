using System.Buffers;
using System.Net.Mqtt.Extensions;
using static System.String;
using static System.Text.Encoding;
using static System.Buffers.Binary.BinaryPrimitives;
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

    public static bool TryRead(ReadOnlySequence<byte> sequence, out ConnectPacket packet, out int consumed)
    {
        if(sequence.IsSingleSegment) return TryRead(sequence.First.Span, out packet, out consumed);

        var sr = new SequenceReader<byte>(sequence);
        return TryRead(ref sr, out packet, out consumed);
    }

    public static bool TryRead(ref SequenceReader<byte> reader, out ConnectPacket packet, out int consumed)
    {
        if(reader.Sequence.IsSingleSegment) return TryRead(reader.UnreadSpan, out packet, out consumed);

        packet = null;
        consumed = 0;

        var remaining = reader.Remaining;

        if(TryReadMqttHeader(ref reader, out var header, out var size) && size <= reader.Remaining &&
           header == 0b0001_0000 && TryReadPayload(ref reader, size, out packet))
        {
            consumed = (int)(remaining - reader.Remaining);
            return true;
        }

        reader.Advance(remaining - reader.Remaining);
        return false;
    }

    public static bool TryRead(ReadOnlySpan<byte> span, out ConnectPacket packet, out int consumed)
    {
        packet = null;
        consumed = 0;

        if(!TryReadMqttHeader(in span, out var header, out var size, out var offset) ||
           offset + size > span.Length || header != 0b0001_0000 ||
           !TryReadPayload(span[offset..], size, out packet))
        {
            return false;
        }

        consumed = offset + size;
        return true;
    }

    public static bool TryReadPayload(ReadOnlySequence<byte> sequence, int size, out ConnectPacket packet)
    {
        packet = null;
        if(sequence.Length < size) return false;

        if(sequence.IsSingleSegment) return TryReadPayload(sequence.First.Span, size, out packet);

        var sr = new SequenceReader<byte>(sequence);
        return TryReadPayload(ref sr, size, out packet);
    }

    public static bool TryReadPayload(ref SequenceReader<byte> reader, int size, out ConnectPacket packet)
    {
        packet = null;
        if(reader.Remaining < size) return false;

        if(reader.Sequence.IsSingleSegment) return TryReadPayload(reader.UnreadSpan, size, out packet);

        var remaining = reader.Remaining;

        if(!TryReadMqttString(ref reader, out var protocol) || !reader.TryRead(out var level) ||
           !reader.TryRead(out var connFlags) || !reader.TryReadBigEndian(out short keepAlive) ||
           !TryReadMqttString(ref reader, out var clientId))
        {
            reader.Rewind(remaining - reader.Remaining);
            return false;
        }

        string topic = null;
        byte[] willMessage = null;
        if((connFlags & 0b0000_0100) == 0b0000_0100)
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
        if((connFlags & 0b1000_0000) == 0b1000_0000 && !TryReadMqttString(ref reader, out userName) ||
           (connFlags & 0b0100_0000) == 0b0100_0000 && !TryReadMqttString(ref reader, out password))
        {
            reader.Rewind(remaining - reader.Remaining);
            return false;
        }

        packet = new ConnectPacket(clientId, level, protocol, (ushort)keepAlive,
            (connFlags & 0b0010) == 0b0010, userName, password, topic, willMessage,
            (byte)((connFlags >> 3) & PacketFlags.QoSMask), (connFlags & 0b0010_0000) == 0b0010_0000);

        return true;
    }

    public static bool TryReadPayload(ReadOnlySpan<byte> span, int size, out ConnectPacket packet)
    {
        packet = null;
        if(span.Length < size) return false;
        if(span.Length > size) span = span[..size];

        if(!TryReadUInt16BigEndian(span, out var len) || span.Length < len + 8) return false;

        var protocol = UTF8.GetString(span.Slice(2, len));
        span = span[(len + 2)..];

        var level = span[0];
        var connFlags = span[1];
        span = span[2..];

        var keepAlive = ReadUInt16BigEndian(span);
        span = span[2..];

        len = ReadUInt16BigEndian(span);
        string clientId = null;
        if(len > 0)
        {
            if(span.Length < len + 2) return false;
            clientId = UTF8.GetString(span.Slice(2, len));
        }

        span = span[(len + 2)..];

        string willTopic = null;
        byte[] willMessage = default;
        if((connFlags & 0b0000_0100) == 0b0000_0100)
        {
            if(!TryReadUInt16BigEndian(span, out len) || len == 0 || span.Length < len + 2) return false;
            willTopic = UTF8.GetString(span.Slice(2, len));
            span = span[(len + 2)..];

            if(!TryReadUInt16BigEndian(span, out len) || span.Length < len + 2) return false;
            if(len > 0)
            {
                willMessage = new byte[len];
                span.Slice(2, len).CopyTo(willMessage);
            }

            span = span[(len + 2)..];
        }

        string userName = null;
        if((connFlags & 0b1000_0000) == 0b1000_0000)
        {
            if(!TryReadUInt16BigEndian(span, out len) || span.Length < len + 2) return false;
            userName = UTF8.GetString(span.Slice(2, len));
            span = span[(len + 2)..];
        }

        string password = null;
        if((connFlags & 0b0100_0000) == 0b0100_0000)
        {
            if(!TryReadUInt16BigEndian(span, out len) || span.Length < len + 2) return false;
            password = UTF8.GetString(span.Slice(2, len));
        }

        packet = new ConnectPacket(clientId, level, protocol, keepAlive,
            (connFlags & 0x2) == 0x2, userName, password, willTopic, willMessage,
            (byte)((connFlags >> 3) & PacketFlags.QoSMask), (connFlags & 0x2_0) == 0x2_0);

        return true;
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
        span[0] = 0b0001_0000;
        span = span[1..];
        // Remaining length bytes
        span = span[WriteMqttLengthBytes(ref span, remainingLength)..];
        // Protocol info bytes
        span = span[WriteMqttString(ref span, ProtocolName)..];
        span[0] = ProtocolLevel;
        // Connection flag
        var flags = (byte)(WillQoS << 3);
        if(hasUserName) flags |= 0b1000_0000;
        if(hasPassword) flags |= 0b0100_0000;
        if(WillRetain) flags |= 0b0010_0000;
        if(hasWillTopic) flags |= 0b0000_0100;
        if(CleanSession) flags |= 0b0000_0010;
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
        if(hasUserName) span = span[WriteMqttString(ref span, UserName)..];
        //Password
        if(hasPassword) WriteMqttString(ref span, Password);
    }

    #endregion
}