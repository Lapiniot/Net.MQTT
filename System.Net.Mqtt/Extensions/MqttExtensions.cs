using System.IO.Pipelines;
using System.Net.Mqtt.Properties;
using static System.Net.Mqtt.Extensions.SequenceExtensions;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt.Extensions;

public static class MqttExtensions
{
    public static int GetLengthByteCount(int length)
    {
        return length == 0 ? 1 : (int)Math.Log(length, 128) + 1;
    }

    public static bool IsValidFilter(ReadOnlySpan<char> filter)
    {
        if(filter.Length is 0) return false;

        var lastIndex = filter.Length - 1;

        for(var i = 0; i < filter.Length; i++)
        {
            switch(filter[i])
            {
                case '+' when i > 0 && filter[i - 1] != '/' || i < lastIndex && filter[i + 1] != '/':
                case '#' when i != lastIndex || i > 0 && filter[i - 1] != '/':
                    return false;
            }
        }

        return true;
    }

    public static bool TopicMatches(ReadOnlySpan<char> topic, ReadOnlySpan<char> filter)
    {
        var tlen = topic.Length;
        var ti = 0;

        for(var fi = 0; fi < filter.Length; fi++)
        {
            var ch = filter[fi];

            if(ti < tlen)
            {
                if(ch != topic[ti])
                {
                    if(ch != '+') return ch == '#';
                    // Scan and skip topic characters until level separator occurrence
                    while(ti < tlen && topic[ti] != '/') ti++;
                    continue;
                }

                ti++;
            }
            else
            {
                // Edge case: we ran out of characters in the topic sequence.
                // Return true only for proper topic filter level wildcard specified.
                return ch == '#' || ch == '+' && topic[tlen - 1] == '/';
            }
        }

        // return true only if topic character sequence has been completely scanned
        return ti == tlen;
    }

    public static async Task<int> DetectProtocolVersionAsync(PipeReader reader, CancellationToken token)
    {
        var (flags, offset, _, buffer) = await MqttPacketHelpers.ReadPacketAsync(reader, token).ConfigureAwait(false);

        if((flags & TypeMask) != 0b0001_0000) throw new InvalidDataException(Strings.ConnectPacketExpected);

        if(!TryReadMqttString(buffer.Slice(offset), out var protocol, out var consumed) ||
           string.IsNullOrEmpty(protocol))
        {
            throw new InvalidDataException(Strings.ProtocolNameExpected);
        }

        if(!TryReadByte(buffer.Slice(offset + consumed), out var level))
        {
            throw new InvalidDataException(Strings.ProtocolVersionExpected);
        }

        // Notify that we have not consumed any data from the pipe and 
        // cancel current pending Read operation to unblock any further 
        // immediate reads. Otherwise next reader will be blocked until 
        // new portion of data is read from network socket and flushed out
        // by writer task. Essentially, this is just a simulation of "Peek"
        // operation in terms of pipelines API.
        reader.AdvanceTo(buffer.Start, buffer.End);
        reader.CancelPendingRead();

        return level;
    }
}