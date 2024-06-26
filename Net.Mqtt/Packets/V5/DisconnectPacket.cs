﻿using static System.Buffers.Binary.BinaryPrimitives;
using static Net.Mqtt.Extensions.SequenceReaderExtensions;
using static Net.Mqtt.Extensions.SpanExtensions;
using static Net.Mqtt.MqttHelpers;

namespace Net.Mqtt.Packets.V5;

public sealed class DisconnectPacket(byte reasonCode) : IMqttPacket5
{
    public byte ReasonCode { get; } = reasonCode;
    public uint SessionExpiryInterval { get; init; }
    public ReadOnlyMemory<byte> ReasonString { get; init; }
    public ReadOnlyMemory<byte> ServerReference { get; init; }
    public IReadOnlyList<UserProperty> UserProperties { get; init; }

    public static bool TryReadPayload(in ReadOnlySequence<byte> sequence, out byte reasonCode, out uint? sessionExpiryInterval,
        out byte[] reasonString, out byte[] serverReference, out IReadOnlyList<UserProperty> properties)
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
        out IReadOnlyList<UserProperty> properties)
    {
        sessionExpiryInterval = null;
        reasonString = null;
        serverReference = null;
        properties = null;
        List<UserProperty> props = null;

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
        out IReadOnlyList<UserProperty> properties)
    {
        sessionExpiryInterval = null;
        reasonString = null;
        serverReference = null;
        properties = null;
        List<UserProperty> props = null;

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
        var userPropertiesSize = GetUserPropertiesSize(UserProperties);
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
                WriteUInt32BigEndian(buffer, (uint)(PacketFlags.DisconnectPacket32 | 0x10000u | ReasonCode << 8));
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
            var count = UserProperties.Count;
            for (var i = 0; i < count; i++)
            {
                var (key, value) = UserProperties[i];
                WriteMqttUserProperty(ref span, key.Span, value.Span);
            }
        }

        writer.Advance(size);
        return size;
    }

    #endregion
}