using OOs.Net.Connections;

namespace Net.Mqtt.Server.AspNetCore.Hosting;

/// <summary>
/// Defines a handler for managing transport connections.
/// </summary>
public interface ITransportConnectionHandler
{
    /// <summary>
    /// Called when a transport connection is established. 
    /// Method should not return until connection is closed by the consumer, 
    /// data processing is complete or explicit cancellation is requested.
    /// </summary>
    /// <param name="connection">The transport connection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask OnConnectedAsync<T>(T connection, CancellationToken cancellationToken)
        where T : TransportConnection, ITransportConnectionLifetime;
}