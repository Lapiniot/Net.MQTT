namespace System.Net.Mqtt.Client
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(string topic, Memory<byte> payload, bool retained)
        {
            Topic = topic;
            Payload = payload;
            Retained = retained;
        }

        public string Topic { get; }
        public Memory<byte> Payload { get; }
        public bool Retained { get; }
    }
}