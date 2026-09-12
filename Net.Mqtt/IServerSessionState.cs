namespace Net.Mqtt;

/// <summary>
/// Defines the common interface for server session state implementation
/// </summary>
public interface IServerSessionState
{
    /// <summary>
    /// Gets or sets a value indicating whether the session is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the client ID of the session.
    /// </summary>
    string ClientId { get; }
}