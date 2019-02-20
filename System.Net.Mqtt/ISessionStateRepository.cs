namespace System.Net.Mqtt
{
    public interface ISessionStateRepository<out T>
    {
        T GetOrCreate(string clientId, bool cleanSession);
        void Remove(string clientId);
    }
}