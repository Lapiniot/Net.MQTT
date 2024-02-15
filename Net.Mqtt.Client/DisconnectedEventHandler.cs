namespace Net.Mqtt.Client;

public class DisconnectedEventArgs(bool aborted, bool tryReconnect) : EventArgs
{
    public bool Aborted { get; } = aborted;
    public bool TryReconnect { get; set; } = tryReconnect;
}