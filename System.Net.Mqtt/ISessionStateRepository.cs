namespace System.Net.Mqtt;

public interface ISessionStateRepository<out T>
{
    T GetOrCreate(string clientId, bool clean, out bool existed);
    void Remove(string clientId);
}