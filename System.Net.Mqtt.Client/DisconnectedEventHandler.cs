namespace System.Net.Mqtt.Client
{
    public delegate void DisconnectedEventHandler(MqttClient sender, DisconnectedEventArgs args);

    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(bool aborted, bool tryReconnect)
        {
            Aborted = aborted;
            TryReconnect = tryReconnect;
        }

        public bool Aborted { get; }
        public bool TryReconnect { get; set; }
    }
}