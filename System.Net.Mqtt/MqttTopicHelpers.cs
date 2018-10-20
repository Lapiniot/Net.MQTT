namespace System.Net.Mqtt
{
    public static class MqttTopicHelpers
    {
        public static bool IsValidTopic(string topic)
        {
            if(string.IsNullOrEmpty(topic)) return false;

            ReadOnlySpan<char> s = topic;

            var lastIndex = s.Length - 1;

            for(var i = 0; i < s.Length; i++)
            {
                var c = s[i];

                if(c == '+' && (i > 0 && s[i - 1] != '/' || i < lastIndex && s[i + 1] != '/'))
                {
                    return false;
                }

                if(c == '#' && (i != lastIndex || i > 0 && s[i - 1] != '/'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}