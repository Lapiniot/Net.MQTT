using static System.Net.Mqtt.PacketFlags;
using SequenceReaderExtensions = System.Net.Mqtt.Extensions.SequenceReaderExtensions;

namespace System.Net.Mqtt.Packets.V3;

public sealed class ConnectPacket : MqttPacket, IBinaryReader<ConnectPacket>
{
    public ConnectPacket(ReadOnlyMemory<byte> clientId, byte protocolLevel, ReadOnlyMemory<byte> protocolName,
        ushort keepAlive = 120, bool cleanSession = true, ReadOnlyMemory<byte> userName = default, ReadOnlyMemory<byte> password = default,
        ReadOnlyMemory<byte> willTopic = default, ReadOnlyMemory<byte> willMessage = default, byte willQoS = 0x00, bool willRetain = false)
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
    public ReadOnlyMemory<byte> UserName { get; }
    public ReadOnlyMemory<byte> Password { get; }
    public ReadOnlyMemory<byte> ClientId { get; }
    public ReadOnlyMemory<byte> WillTopic { get; }
    public ReadOnlyMemory<byte> WillMessage { get; }
    public byte WillQoS { get; }
    public bool WillRetain { get; }
    public bool CleanSession { get; }
    public ReadOnlyMemory<byte> ProtocolName { get; }
    public byte ProtocolLevel { get; }

    internal int PayloadSize => 2 + ClientId.Length +
                                (UserName.IsEmpty ? 0 : 2 + UserName.Length) +
                                (Password.IsEmpty ? 0 : 2 + Password.Length) +
                                (WillTopic.IsEmpty ? 0 : 4 + WillTopic.Length + WillMessage.Length);

    internal int HeaderSize => 6 + ProtocolName.Length;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out ConnectPacket value, out int consumed)
    {
        value = null;
        consumed = 0;

        if (sequence.IsSingleSegment && TryRead(sequence.FirstSpan, out value, out consumed))
            return true;

        var reader = new SequenceReader<byte>(sequence);

        if (SequenceReaderExtensions.TryReadMqttHeader(ref reader, out var header, out var size) && size <= reader.Remaining && header == ConnectMask)
        {
            if (!SequenceReaderExtensions.TryReadMqttString(ref reader, out var protocol) || !reader.TryRead(out var level) ||
                !reader.TryRead(out var connFlags) || !reader.TryReadBigEndian(out short keepAlive) ||
                !SequenceReaderExtensions.TryReadMqttString(ref reader, out var clientId))
            {
                return false;
            }

            byte[] topic = null;
            byte[] willMessage = null;

            if ((connFlags & WillMask) == WillMask)
            {
                if (!SequenceReaderExtensions.TryReadMqttString(ref reader, out topic) || !reader.TryReadBigEndian(out short v16))
                    return false;

                var willSize = (ushort)v16;

                if (willSize > 0)
                {
                    willMessage = new byte[willSize];

                    if (!reader.TryCopyTo(willMessage))
                        return false;

                    reader.Advance(willSize);
                }
            }

            byte[] userName = null;
            byte[] password = null;

            if ((connFlags & UserNameMask) == UserNameMask && !SequenceReaderExtensions.TryReadMqttString(ref reader, out userName) ||
                (connFlags & PasswordMask) == PasswordMask && !SequenceReaderExtensions.TryReadMqttString(ref reader, out password))
            {
                return false;
            }

            value = new(clientId, level, protocol, (ushort)keepAlive,
                (connFlags & CleanSessionMask) == CleanSessionMask, userName, password, topic, willMessage,
                (byte)(connFlags >> 3 & QoSMask), (connFlags & WillRetainMask) == WillRetainMask);
            return true;
        }

        return false;
    }

    private static bool TryRead(in ReadOnlySpan<byte> span, out ConnectPacket packet, out int consumed)
    {
        packet = null;
        consumed = 0;

        if (SpanExtensions.TryReadMqttHeader(span, out var header, out var size, out var offset) &&
            offset + size <= span.Length && header == ConnectMask)
        {
            var current = span.Slice(offset, size);

            if (!BinaryPrimitives.TryReadUInt16BigEndian(current, out var len) || current.Length < len + 8) return false;

            var protocol = current.Slice(2, len).ToArray();
            current = current.Slice(len + 2);
            var level = current[0];
            var connFlags = current[1];
            current = current.Slice(2);

            var keepAlive = BinaryPrimitives.ReadUInt16BigEndian(current);
            current = current.Slice(2);
            len = BinaryPrimitives.ReadUInt16BigEndian(current);
            ReadOnlyMemory<byte> clientId = default;

            if (len > 0)
            {
                if (current.Length < len + 2) return false;
                clientId = current.Slice(2, len).ToArray();
            }

            current = current.Slice(len + 2);
            ReadOnlyMemory<byte> willTopic = default;
            byte[] willMessage = default;

            if ((connFlags & WillMask) == WillMask)
            {
                if (!BinaryPrimitives.TryReadUInt16BigEndian(current, out len) || len == 0 || current.Length < len + 2) return false;
                willTopic = current.Slice(2, len).ToArray();
                current = current.Slice(len + 2);

                if (!BinaryPrimitives.TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;

                if (len > 0)
                {
                    willMessage = new byte[len];
                    current.Slice(2, len).CopyTo(willMessage);
                }

                current = current.Slice(len + 2);
            }

            ReadOnlyMemory<byte> userName = default;

            if ((connFlags & UserNameMask) == UserNameMask)
            {
                if (!BinaryPrimitives.TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                userName = current.Slice(2, len).ToArray();
                current = current.Slice(len + 2);
            }

            ReadOnlyMemory<byte> password = default;

            if ((connFlags & PasswordMask) == PasswordMask)
            {
                if (!BinaryPrimitives.TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                password = current.Slice(2, len).ToArray();
            }

            packet = new(clientId, level, protocol, keepAlive,
                (connFlags & CleanSessionMask) == CleanSessionMask, userName, password, willTopic, willMessage,
                (byte)(connFlags >> 3 & QoSMask), (connFlags & WillRetainMask) == WillRetainMask);
            return true;
        }

        consumed = 0;
        return false;
    }

    #region Overrides of MqttPacket

    public override int GetSize(out int remainingLength)
    {
        remainingLength = HeaderSize + PayloadSize;
        return 1 + MqttExtensions.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        var hasClientId = !ClientId.IsEmpty;
        var hasUserName = !UserName.IsEmpty;
        var hasPassword = !Password.IsEmpty;
        var hasWillTopic = !WillTopic.IsEmpty;
        var flags = (byte)(WillQoS << 3);
        if (hasUserName) flags |= UserNameMask;
        if (hasPassword) flags |= PasswordMask;
        if (WillRetain) flags |= WillRetainMask;
        if (hasWillTopic) flags |= WillMask;
        if (CleanSession) flags |= CleanSessionMask;

        // Packet flags
        span[0] = ConnectMask;
        span = span.Slice(1);
        // Remaining length bytes
        SpanExtensions.WriteMqttVarByteInteger(ref span, remainingLength);
        // Protocol info bytes
        SpanExtensions.WriteMqttString(ref span, ProtocolName.Span);
        span[1] = flags;
        span[0] = ProtocolLevel;
        span = span.Slice(2);
        // KeepAlive bytes
        BinaryPrimitives.WriteUInt16BigEndian(span, KeepAlive);
        span = span.Slice(2);

        // Payload bytes
        if (hasClientId)
        {
            SpanExtensions.WriteMqttString(ref span, ClientId.Span);
        }
        else
        {
            span[1] = 0;
            span[0] = 0;
            span = span.Slice(2);
        }

        // Last will
        if (hasWillTopic)
        {
            SpanExtensions.WriteMqttString(ref span, WillTopic.Span);
            var messageSpan = WillMessage.Span;
            var spanLength = messageSpan.Length;
            BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)spanLength);
            span = span.Slice(2);
            messageSpan.CopyTo(span);
            span = span.Slice(spanLength);
        }

        // Username
        if (hasUserName)
            SpanExtensions.WriteMqttString(ref span, UserName.Span);

        //Password
        if (hasPassword)
            SpanExtensions.WriteMqttString(ref span, Password.Span);
    }

    #endregion
}