namespace System.Net.Mqtt.Client
{
    public delegate void ConnectedEventHandler(MqttClient sender, ConnectedEventArgs args);

    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(bool cleanSession)
        {
            CleanSession = cleanSession;
        }

        public bool CleanSession { get; }
    }
}