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

    public static bool IsValidFilter(string filter)
    {
        if(string.IsNullOrEmpty(filter)) return false;

        ReadOnlySpan<char> s = filter;

        var lastIndex = s.Length - 1;

        for(var i = 0; i < s.Length; i++)
        {
            switch(s[i])
            {
                case '+' when i > 0 && s[i - 1] != '/' || i < lastIndex && s[i + 1] != '/':
                case '#' when i != lastIndex || i > 0 && s[i - 1] != '/':
                    return false;
            }
        }

        return true;
    }

    public static bool TopicMatches(string topic, string filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if(string.IsNullOrEmpty(topic)) return false;

        if(filter.Length is 1 && filter[0] is '#')
        {
            return true;
        }

        ReadOnlySpan<char> t = topic;
        ReadOnlySpan<char> f = filter;

        var length = topic.Length;
        var index = 0;

        for(var i = 0; i < filter.Length; i++)
        {
            var current = f[i];

            if(index < length)
            {
                if(current != t[index])
                {
                    if(current != '+') return current == '#';
                    // Scan and skip topic characters until level separator occurrence
                    while(index < length && t[index] != '/') index++;
                    continue;
                }

                index++;
            }
            else
            {
                // Edge case: we ran out of characters in the topic sequence.
                // Return true only for proper topic filter level wildcard specified.
                return current == '#' || current == '+' && t[length - 1] == '/';
            }
        }

        // return true only if topic character sequence has been completely scanned
        return index == length;
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