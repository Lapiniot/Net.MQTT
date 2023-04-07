namespace System.Net.Mqtt.Server;

public sealed partial class MqttServer : IConnectionInfoFeature, IAbortConnectionFeature, IObservable<ConnectionStateChangedMessage>
{
    private readonly ObserversContainer<ConnectionStateChangedMessage> connStateObservers;

    private Channel<ConnectionStateChangedMessage> connStateMessageQueue;
    private int totalSessions;

    #region IConnectionInfoFeature implementation

    IEnumerable<ConnectionInfo> IConnectionInfoFeature.GetConnections()
    {
        var list = new List<ConnectionInfo>(activeConnections);
        foreach (var (clientId, (conn, _, created)) in connections)
        {
            list.Add(new ConnectionInfo(clientId, conn.Id, conn.LocalEndPoint, conn.RemoteEndPoint, created));
        }

        return list;
    }

    #endregion

    #region IObservable<ConnectionStateChangedMessage> implementation

    IDisposable IObservable<ConnectionStateChangedMessage>.Subscribe(IObserver<ConnectionStateChangedMessage> observer) =>
        connStateObservers.Subscribe(observer);

    #endregion

    private async Task RunConnectionStateNotifierAsync(CancellationToken stoppingToken)
    {
        // Cache connections dictionary enumerator to avoid excessive memory allocations
        var enumerator = connections.GetEnumerator();
        var reader = connStateMessageQueue.Reader;
        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (!stoppingToken.IsCancellationRequested && reader.TryRead(out var message))
                {
                    connStateObservers.Notify(message);
                }

                if (!RuntimeSettings.MetricsCollectionSupport)
                {
                    continue;
                }

                // Aggregate all metrics provided by active connection-session contexts
                var total = 0;
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    total++;
                }

                activeConnections = total;

                // Aggregate all metrics provided by running protocol hubs
                total = 0;
                foreach (var hub in hubs.Values)
                {
                    if (hub is ISessionStatisticsFeature statistics)
                        total += statistics.GetTotalSessions();
                }

                totalSessions = total;

                // Pulse to trigger subscriptions aggregation task
                updateStatsSignal.TrySetResult();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    #region IAbortConnectionFeature implementation

    void IAbortConnectionFeature.Abort(string clientId)
    {
        if (connections.TryGetValue(clientId, out var ctx))
        {
            ctx.Abort();
        }
    }

    #endregion
}