using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Client;

public class ConnectedEventArgs : EventArgs
{
    private static ConnectedEventArgs cleanSessionInstance;
    private static ConnectedEventArgs existingSessionInstance;

    public ConnectedEventArgs(bool cleanSession) => CleanSession = cleanSession;

    public bool CleanSession { get; }

    public static ConnectedEventArgs ExistingSessionInstance => existingSessionInstance ??= new(false);

    public static ConnectedEventArgs CleanSessionInstance => cleanSessionInstance ??= new(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConnectedEventArgs GetInstance(bool cleanSession) => cleanSession ? CleanSessionInstance : ExistingSessionInstance;
}