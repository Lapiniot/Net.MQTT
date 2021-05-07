namespace System.Net.Mqtt.Client
{
    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(bool cleanSession)
        {
            CleanSession = cleanSession;
        }

        public bool CleanSession { get; }
    }
}