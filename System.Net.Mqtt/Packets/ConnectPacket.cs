using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Packets;

public class ConnectPacket : MqttPacket
{
    public ConnectPacket(Utf8String clientId, byte protocolLevel, Utf8String protocolName,
        ushort keepAlive = 120, bool cleanSession = true, Utf8String userName = default, Utf8String password = default,
        Utf8String willTopic = default, MqttPayload willMessage = default, byte willQoS = 0x00, bool willRetain = false)
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
    public Utf8String UserName { get; }
    public Utf8String Password { get; }
    public Utf8String ClientId { get; }
    public Utf8String WillTopic { get; }
    public MqttPayload WillMessage { get; }
    public byte WillQoS { get; }
    public bool WillRetain { get; }
    public bool CleanSession { get; }
    public Utf8String ProtocolName { get; }
    public byte ProtocolLevel { get; }

    protected internal int PayloadSize
    {
        get
        {
            return 2 + ClientId.Length +
                   (UserName.IsEmpty ? 0 : 2 + UserName.Length) +
                   (Password.IsEmpty ? 0 : 2 + Password.Length) +
                   (WillTopic.IsEmpty ? 0 : 4 + WillTopic.Length + WillMessage.Length);
        }
    }

    protected internal int HeaderSize => 6 + ProtocolName.Length;

    public static bool TryRead(in ReadOnlySequence<byte> sequence, out ConnectPacket packet, out int consumed)
    {
        if (TryRead(sequence.FirstSpan, out packet, out consumed))
        {
            return true;
        }

        var reader = new SequenceReader<byte>(sequence);

        var remaining = reader.Remaining;

        if (SRE.TryReadMqttHeader(ref reader, out var header, out var size) && size <= reader.Remaining && header == ConnectMask)
        {
            if (!SRE.TryReadMqttString(ref reader, out var protocol) || !reader.TryRead(out var level) ||
                !reader.TryRead(out var connFlags) || !reader.TryReadBigEndian(out short keepAlive) ||
                !SRE.TryReadMqttString(ref reader, out var clientId))
            {
                reader.Rewind(remaining - reader.Remaining);
                return false;
            }

            Utf8String topic = default;
            byte[] willMessage = null;
            if ((connFlags & WillMask) == WillMask)
            {
                if (!SRE.TryReadMqttString(ref reader, out topic) || !reader.TryReadBigEndian(out short value))
                {
                    reader.Rewind(remaining - reader.Remaining);
                    return false;
                }

                var willSize = (ushort)value;
                if (willSize > 0)
                {
                    willMessage = new byte[willSize];
                    if (!reader.TryCopyTo(willMessage)) return false;
                    reader.Advance(willSize);
                }
            }

            Utf8String userName = default;
            Utf8String password = default;
            if ((connFlags & UserNameMask) == UserNameMask && !SRE.TryReadMqttString(ref reader, out userName) ||
                (connFlags & PasswordMask) == PasswordMask && !SRE.TryReadMqttString(ref reader, out password))
            {
                reader.Rewind(remaining - reader.Remaining);
                return false;
            }

            packet = new(clientId, level, protocol, (ushort)keepAlive,
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

        if (SPE.TryReadMqttHeader(in span, out var header, out var size, out var offset) &&
            offset + size <= span.Length && header == ConnectMask)
        {
            var current = span.Slice(offset, size);

            if (!BP.TryReadUInt16BigEndian(current, out var len) || current.Length < len + 8) return false;

            var protocol = current.Slice(2, len).ToArray();
            current = current[(len + 2)..];

            var level = current[0];
            var connFlags = current[1];
            current = current[2..];

            var keepAlive = BP.ReadUInt16BigEndian(current);
            current = current[2..];

            len = BP.ReadUInt16BigEndian(current);
            Utf8String clientId = default;
            if (len > 0)
            {
                if (current.Length < len + 2) return false;
                clientId = current.Slice(2, len).ToArray();
            }

            current = current[(len + 2)..];

            Utf8String willTopic = default;
            byte[] willMessage = default;
            if ((connFlags & WillMask) == WillMask)
            {
                if (!BP.TryReadUInt16BigEndian(current, out len) || len == 0 || current.Length < len + 2) return false;
                willTopic = current.Slice(2, len).ToArray();
                current = current[(len + 2)..];

                if (!BP.TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                if (len > 0)
                {
                    willMessage = new byte[len];
                    current.Slice(2, len).CopyTo(willMessage);
                }

                current = current[(len + 2)..];
            }

            Utf8String userName = default;
            if ((connFlags & UserNameMask) == UserNameMask)
            {
                if (!BP.TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                userName = current.Slice(2, len).ToArray();
                current = current[(len + 2)..];
            }

            Utf8String password = default;
            if ((connFlags & PasswordMask) == PasswordMask)
            {
                if (!BP.TryReadUInt16BigEndian(current, out len) || current.Length < len + 2) return false;
                password = current.Slice(2, len).ToArray();
            }

            packet = new(clientId, level, protocol, keepAlive,
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
        remainingLength = HeaderSize + PayloadSize;
        return 1 + ME.GetLengthByteCount(remainingLength) + remainingLength;
    }

    public override void Write(Span<byte> span, int remainingLength)
    {
        var hasClientId = !ClientId.IsEmpty;
        var hasUserName = !UserName.IsEmpty;
        var hasPassword = !Password.IsEmpty;
        var hasWillTopic = !WillTopic.IsEmpty;

        // Packet flags
        span[0] = ConnectMask;
        span = span[1..];
        // Remaining length bytes
        span = span[SPE.WriteMqttLengthBytes(ref span, remainingLength)..];
        // Protocol info bytes
        span = span[SPE.WriteMqttString(ref span, ProtocolName.Span)..];
        span[0] = ProtocolLevel;
        // Connection flag
        var flags = (byte)(WillQoS << 3);
        if (hasUserName) flags |= UserNameMask;
        if (hasPassword) flags |= PasswordMask;
        if (WillRetain) flags |= WillRetainMask;
        if (hasWillTopic) flags |= WillMask;
        if (CleanSession) flags |= CleanSessionMask;
        span[1] = flags;
        span = span[2..];
        // KeepAlive bytes
        BP.WriteUInt16BigEndian(span, KeepAlive);
        span = span[2..];
        // Payload bytes
        if (hasClientId)
        {
            span = span[SPE.WriteMqttString(ref span, ClientId.Span)..];
        }
        else
        {
            span[0] = 0;
            span[1] = 0;
            span = span[2..];
        }

        // Last will
        if (hasWillTopic)
        {
            span = span[SPE.WriteMqttString(ref span, WillTopic.Span)..];
            var messageSpan = WillMessage.Span;
            var spanLength = messageSpan.Length;
            BP.WriteUInt16BigEndian(span, (ushort)spanLength);
            span = span[2..];
            messageSpan.CopyTo(span);
            span = span[spanLength..];
        }

        // Username
        if (hasUserName)
        {
            span = span[SPE.WriteMqttString(ref span, UserName.Span)..];
        }

        //Password
        if (hasPassword)
        {
            SPE.WriteMqttString(ref span, Password.Span);
        }
    }

    #endregion
}