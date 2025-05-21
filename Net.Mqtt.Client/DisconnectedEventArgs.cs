namespace Net.Mqtt.Client;

/// <summary>
/// Event arguments for the <see cref="MqttClient.Disconnected"/> event.
/// This event is raised when the client is disconnected from the broker.
/// </summary>
/// <param name="graceful">
/// Indicates whether the disconnection was graceful (<seealso cref="MqttClient.DisconnectAsync"/> 
/// was called explicitly by the user code).
/// </param>
public class DisconnectedEventArgs(bool graceful) : EventArgs
{
    public bool Graceful { get; } = graceful;
}