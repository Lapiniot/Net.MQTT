namespace System.Net.Mqtt.Extensions
{
    public static class MqttExtensions
    {
        public static int GetLengthByteCount(int length)
        {
            return length == 0 ? 1 : (int)Math.Log(length, 128) + 1;
        }

        public static bool IsValidTopic(string topic)
        {
            if(string.IsNullOrEmpty(topic)) return false;

            ReadOnlySpan<char> s = topic;

            var lastIndex = s.Length - 1;

            for(var i = 0; i < s.Length; i++)
            {
                var c = s[i];

                switch(c)
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
            if(filter == null) throw new ArgumentNullException(nameof(filter));
            if(string.IsNullOrEmpty(topic)) return false;

            if(filter == "#") return true;

            ReadOnlySpan<char> t = topic;
            ReadOnlySpan<char> f = filter;

            var topicLength = topic.Length;

            var topicIndex = 0;

            for(var index = 0; index < filter.Length; index++)
            {
                var current = f[index];

                if(topicIndex < topicLength)
                {
                    if(current != t[topicIndex])
                    {
                        if(current != '+') return current == '#';
                        // Scan and skip topic characters until level separator occurence
                        while(topicIndex < topicLength && t[topicIndex] != '/') topicIndex++;
                        continue;
                    }

                    topicIndex++;
                }
                else
                {
                    // Edge case: we ran out of characters in the topic sequence.
                    // Return true only for proper topic filter level wildcard specified.
                    return current == '#' || current == '+' && t[topicLength - 1] == '/';
                }
            }

            // return true only if topic character sequence has been completely scanned
            return topicIndex == topicLength;
        }
    }
}