using System.Threading.Channels;

namespace System.Net.Mqtt.Extensions;

public static class ChannelsExtensions
{
    public static void Deconstruct<T>(this Channel<T> channel, out ChannelReader<T> reader, out ChannelWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(channel);

        reader = channel.Reader;
        writer = channel.Writer;
    }
}