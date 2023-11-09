namespace System.Net.Mqtt.Client;

public class ConnectedEventArgs(bool clean) : EventArgs
{
    private static ConnectedEventArgs cleanSessionInstance;
    private static ConnectedEventArgs existingSessionInstance;

    public bool Clean { get; } = clean;

    public static ConnectedEventArgs ExistingSessionInstance => existingSessionInstance ??= new(false);

    public static ConnectedEventArgs CleanSessionInstance => cleanSessionInstance ??= new(true);

    public static ConnectedEventArgs GetInstance(bool cleanSession) => cleanSession ? CleanSessionInstance : ExistingSessionInstance;
}