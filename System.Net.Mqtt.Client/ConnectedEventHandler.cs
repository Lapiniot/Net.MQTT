namespace System.Net.Mqtt.Client;

public class ConnectedEventArgs(bool cleanSession) : EventArgs
{
    private static ConnectedEventArgs cleanSessionInstance;
    private static ConnectedEventArgs existingSessionInstance;

    public bool CleanSession { get; } = cleanSession;

    public static ConnectedEventArgs ExistingSessionInstance => existingSessionInstance ??= new(false);

    public static ConnectedEventArgs CleanSessionInstance => cleanSessionInstance ??= new(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConnectedEventArgs GetInstance(bool cleanSession) => cleanSession ? CleanSessionInstance : ExistingSessionInstance;
}