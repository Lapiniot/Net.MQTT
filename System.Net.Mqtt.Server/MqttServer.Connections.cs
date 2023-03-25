namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IProvideConnectionsInfo, IManageConnections, IObservable<ConnectionStateChangedMessage>
{
    private readonly ObserversContainer<ConnectionStateChangedMessage> connStateObservers;

    private Channel<ConnectionStateChangedMessage> connStateMessageQueue;

    #region IProvideConnectionsInfo implementation

    IReadOnlyList<ConnectionInfo> IProvideConnectionsInfo.GetConnections()
    {
        var list = new List<ConnectionInfo>(connections.Count);
        foreach (var (clientId, (conn, _, created)) in connections)
        {
            list.Add(new ConnectionInfo(clientId, conn.Id, conn.LocalEndPoint, conn.RemoteEndPoint, created));
        }

        return list.AsReadOnly();
    }

    #endregion

    #region IObservable<ConnectionStateChangedMessage> implementation

    IDisposable IObservable<ConnectionStateChangedMessage>.Subscribe(IObserver<ConnectionStateChangedMessage> observer) =>
        connStateObservers.Subscribe(observer);

    #endregion

    private async Task RunConnectionStateNotifierAsync(CancellationToken stoppingToken)
    {
        var reader = connStateMessageQueue.Reader;
        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (!stoppingToken.IsCancellationRequested && reader.TryRead(out var message))
                {
                    connStateObservers.Notify(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    #region IManageConnections implementation

    void IManageConnections.Abort(string clientId)
    {
        if (connections.TryGetValue(clientId, out var ctx))
        {
            ctx.Abort();
        }
    }

    #endregion
}