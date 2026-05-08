namespace Net.Mqtt.Server.AspNetCore.Hosting;

/// <summary>
/// Provides capabilities to track the lifetime of a transport connection.
/// </summary>
public interface ITransportConnectionLifetime
{
    /// <summary>
    /// Gets a task that represents the completion of the transport connection.
    /// When task is completed, all work on this transport is done so underlying 
    /// connection can be disposed by Kestrel connection manager.
    /// </summary>
    Task Completed { get; }

    /// <summary>
    /// Gets a task that represents the closure of the network activity on the underlaying connection.
    /// </summary>
    Task ConnectionClosed { get; }
}