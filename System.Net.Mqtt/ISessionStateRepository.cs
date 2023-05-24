namespace System.Net.Mqtt;

/// <summary>
/// Defines basic session state repository
/// </summary>
/// <typeparam name="T">Type of the state this repository stores</typeparam>
public interface ISessionStateRepository<out T> where T : MqttSessionState
{
    /// <summary>
    /// Gets instance of the stored state or creates new one if needed/requested.
    /// </summary>
    /// <param name="clientId">ClientId which uniquely idetifies state.</param>
    /// <param name="clean">Force creation of the new instance even if any exists already.</param>
    /// <param name="exists">Flag indicates whether session state exists in the repository.</param>
    /// <returns>Instance of the session state associated with <paramref name="clientId" />.</returns>
    T Acquire(string clientId, bool clean, out bool exists);

    /// <summary>
    /// Deliberatly removes state associated with <paramref name="clientId" /> from the storage.
    /// </summary>
    /// <param name="clientId">ClientId which uniquely idetifies state.</param>
    void Discard(string clientId);

    /// <summary>
    /// Exempts session state from usage by the session, marks it inactive and schedules its 
    /// complete discard after <paramref name="discardInactiveAfter" /> only if it's not currently in use.
    /// </summary>
    /// <param name="clientId">ClientId which uniquely idetifies state.</param>
    /// <param name="discardInactiveAfter">Delay after which inactive session state must be completely discarded.</param>
    /// <remarks>Passing <see cref="Timeout.InfiniteTimeSpan" /> should keep session state in the storage infinitely 
    /// unless it is forcibly removed by the call to the <see cref="Discard(string)" /></remarks>
    void Exempt(string clientId, TimeSpan discardInactiveAfter);
}