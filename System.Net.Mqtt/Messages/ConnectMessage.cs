namespace System.Net.Mqtt.Messages
{
    public class ConnectMessage : MqttMessage
    {
        public short KeepAlive { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; }
        public string WillTopic { get; set; }
        public string WillMessage { get; set; }

        public override Memory<byte> GetBytes()
        {
            throw new NotImplementedException();
        }

        public override int GetSize()
        {
            throw new NotImplementedException();
        }
    }
}