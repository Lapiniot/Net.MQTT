using System.Threading.Channels;

#pragma warning disable CA1034 // Nested types should not be visible

namespace Net.Mqtt.Extensions;

public static class ChannelsExtensions
{
    extension<T>(Channel<T> channel)
    {
        public void Deconstruct(out ChannelReader<T> reader, out ChannelWriter<T> writer)
        {
            ArgumentNullException.ThrowIfNull(channel);

            reader = channel.Reader;
            writer = channel.Writer;
        }
    }
}