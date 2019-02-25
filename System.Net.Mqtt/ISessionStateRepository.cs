namespace System.Net.Mqtt
{
    public interface ISessionStateRepository<out T>
    {
        T GetOrCreate(string clientId, bool cleanSession, out bool existingSession);
        void Remove(string clientId);
    }
}