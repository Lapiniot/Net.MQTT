namespace System.Net.Mqtt.Client
{
    public class MqttConnectionOptions
    {
        public short KeepAlive { get; set; } = 60;
        public bool CleanSession { get; set; } = true;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string LastWillTopic { get; set; }
        public string LastWillMessage { get; set; }
        public QoSLevel LastWillQoS { get; set; }
        public bool LastWillRetain { get; set; }
    }
}