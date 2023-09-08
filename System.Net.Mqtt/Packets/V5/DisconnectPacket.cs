using static System.Buffers.Binary.BinaryPrimitives;
using static System.Net.Mqtt.Extensions.SequenceReaderExtensions;
using static System.Net.Mqtt.Extensions.SpanExtensions;
using static System.Net.Mqtt.MqttHelpers;

namespace System.Net.Mqtt.Packets.V5;

public sealed class DisconnectPacket(byte reasonCode) : IMqttPacket5
{
    #region Disconnect Reason Codes
    public const byte Normal = 0x00;
    public const byte DisconnectWithWillMessage = 0x04;
    public const byte UnspecifiedError = 0x80;
    public const byte MalformedPacket = 0x81;
    public const byte ProtocolError = 0x82;
    public const byte ImplementationSpecificError = 0x83;
    public const byte NotAuthorized = 0x87;
    public const byte ServerBusy = 0x89;
    public const byte ServerShuttingDown = 0x8B;
    public const byte KeepAliveTimeout = 0x8D;
    public const byte SessionTakenOver = 0x8E;
    public const byte TopicFilterInvalid = 0x8F;
    public const byte TopicNameInvalid = 0x90;
    public const byte ReceiveMaximumExceeded = 0x93;
    public const byte TopicAliasInvalid = 0x94;
    public const byte PacketTooLarge = 0x95;
    public const byte MessageRateTooHigh = 0x96;
    public const byte QuotaExceeded = 0x97;
    public const byte AdministrativeAction = 0x98;
    public const byte PayloadFormatInvalid = 0x99;
    public const byte RetainNotSupported = 0x9A;
    public const byte QoSNotSupported = 0x9B;
    public const byte UseAnotherServer = 0x9C;
    public const byte ServerMoved = 0x9D;
    public const byte SharedSubscriptionsNotSupported = 0x9E;
    public const byte ConnectionRateExceeded = 0x9F;
    public const byte MaximumConnectTime = 0xA0;
    public const byte SubscriptionIdentifiersNotSupported = 0xA1;
    public const byte WildcardSubscriptionsNotSupported = 0xA2;

    #endregion

    public byte ReasonCode { get; } = reasonCode;
    public uint SessionExpiryInterval { get; init; }
    public ReadOnlyMemory<byte> ReasonString { get; init; }
    public ReadOnlyMemory<byte> ServerReference { get; init; }
    public IReadOnlyList<Utf8StringPair> Properties { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, out byte reasonCode, out uint? sessionExpiryInterval,
        out byte[] reasonString, out byte[] serverReference, out IReadOnlyList<Utf8StringPair> properties)
    {
        reasonCode = 0;
        sessionExpiryInterval = null;
        reasonString = null;
        serverReference = null;
        properties = null;

        if (sequence.IsSingleSegment)
        {
            var span = sequence.FirstSpan;

            if (span.IsEmpty)
                return true;

            reasonCode = span[0];

            if (span.Length is 1)
                return true;

            span = span.Slice(1);
            if (!TryReadMqttVarByteInteger(span, out var count, out var consumed) || span.Length < count + consumed ||
                !TryReadProperties(span.Slice(consumed, count), out sessionExpiryInterval, out reasonString, out serverReference, out properties))
            {
                return false;
            }
        }
        else
        {
            if (sequence.IsEmpty)
                return true;

            var reader = new SequenceReader<byte>(sequence);

            if (!reader.TryRead(out reasonCode))
                return false;

            if (reader.End)
                return true;

            if (!TryReadMqttVarByteInteger(ref reader, out var count) || count > reader.Remaining ||
                !TryReadProperties(sequence.Slice(reader.Consumed, count), out sessionExpiryInterval, out reasonString, out serverReference, out properties))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryReadProperties(ReadOnlySpan<byte> span,
        out uint? sessionExpiryInterval, out byte[] reasonString, out byte[] serverReference,
        out IReadOnlyList<Utf8StringPair> properties)
    {
        sessionExpiryInterval = null;
        reasonString = null;
        serverReference = null;
        properties = null;
        List<Utf8StringPair> props = null;

        while (span.Length > 0)
        {
            switch (span[0])
            {
                case 0x11:
                    if (sessionExpiryInterval is { } || !TryReadUInt32BigEndian(span.Slice(1), out var v32))
                        return false;
                    sessionExpiryInterval = v32;
                    span = span.Slice(5);
                    break;
                case 0x1c:
                    if (serverReference is { } || !TryReadMqttString(span.Slice(1), out var value, out var count))
                        return false;
                    span = span.Slice(count + 1);
                    serverReference = value;
                    break;
                case 0x1f:
                    if (reasonString is { } || !TryReadMqttString(span.Slice(1), out value, out count))
                        return false;
                    span = span.Slice(count + 1);
                    reasonString = value;
                    break;
                case 0x26:
                    if (!TryReadMqttString(span.Slice(1), out var key, out count))
                        return false;
                    span = span.Slice(count + 1);
                    if (!TryReadMqttString(span, out value, out count))
                        return false;
                    span = span.Slice(count);
                    (props ??= []).Add(new(key, value));
                    break;
                default: return false;
            }
        }

        properties = props?.AsReadOnly();
        return true;
    }

    private static bool TryReadProperties(in ReadOnlySequence<byte> sequence,
        out uint? sessionExpiryInterval, out byte[] reasonString, out byte[] serverReference,
        out IReadOnlyList<Utf8StringPair> properties)
    {
        sessionExpiryInterval = null;
        reasonString = null;
        serverReference = null;
        properties = null;
        List<Utf8StringPair> props = null;

        var reader = new SequenceReader<byte>(sequence);
        while (reader.TryRead(out var id))
        {
            switch (id)
            {
                case 0x11:
                    if (sessionExpiryInterval is { } || !reader.TryReadBigEndian(out int v32))
                        return false;
                    sessionExpiryInterval = (uint)v32;
                    break;
                case 0x1c:
                    if (serverReference is { } || !TryReadMqttString(ref reader, out var value))
                        return false;
                    serverReference = value;
                    break;
                case 0x1f:
                    if (reasonString is { } || !TryReadMqttString(ref reader, out value))
                        return false;
                    reasonString = value;
                    break;
                case 0x26:
                    if (!TryReadMqttString(ref reader, out var key) || !TryReadMqttString(ref reader, out value))
                        return false;
                    (props ??= []).Add(new(key, value));
                    break;
                default: return false;
            }
        }

        properties = props?.AsReadOnly();
        return true;
    }

    #region Implementation of IMqttPacket

    public int Write([NotNull] IBufferWriter<byte> writer, int maxAllowedBytes)
    {
        var reasonStringSize = ReasonString.Length is not 0 and var rsLen ? 3 + rsLen : 0;
        var userPropertiesSize = GetUserPropertiesSize(Properties);
        var propsSize = (SessionExpiryInterval is not 0 ? 5 : 0) +
            reasonStringSize + userPropertiesSize +
            (ServerReference.Length is not 0 and var len ? 3 + len : 0);

        if (propsSize is 0)
        {
            if (ReasonCode is 0)
            {
                var buffer = writer.GetSpan(2);
                WriteUInt16BigEndian(buffer, PacketFlags.DisconnectPacket16);
                writer.Advance(2);
                return 2;
            }
            else
            {
                var buffer = writer.GetSpan(4);
                WriteUInt32BigEndian(buffer, (uint)(PacketFlags.DisconnectPacket32 | 0x10000u | (ReasonCode << 8)));
                writer.Advance(3);
                return 3;
            }
        }

        var size = ComputeAdjustedSizes(maxAllowedBytes, 1, ref propsSize, ref reasonStringSize, ref userPropertiesSize, out var remainingLength);

        if (size > maxAllowedBytes)
            return 0;

        var span = writer.GetSpan(size);

        span[0] = PacketFlags.DisconnectMask;
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, remainingLength);
        span[0] = ReasonCode;
        span = span.Slice(1);
        WriteMqttVarByteInteger(ref span, propsSize);

        if (SessionExpiryInterval is not 0)
        {
            span[0] = 0x11;
            WriteUInt32BigEndian(span = span.Slice(1), SessionExpiryInterval);
            span = span.Slice(4);
        }

        if (reasonStringSize is not 0)
        {
            span[0] = 0x1F;
            span = span.Slice(1);
            WriteMqttString(ref span, ReasonString.Span);
        }

        if (!ServerReference.IsEmpty)
        {
            span[0] = 0x1c;
            span = span.Slice(1);
            WriteMqttString(ref span, ServerReference.Span);
        }

        if (userPropertiesSize is not 0)
        {
            var count = Properties.Count;
            for (var i = 0; i < count; i++)
            {
                var (key, value) = Properties[i];
                WriteMqttUserProperty(ref span, key.Span, value.Span);
            }
        }

        writer.Advance(size);
        return size;
    }

    #endregion
}