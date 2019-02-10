namespace System.Net.Mqtt.Server
{
    public interface ISessionStateRepository<out T> where T : SessionState
    {
        T GetOrCreate(string clientId, bool cleanSession);
        void Remove(string clientId);
    }
}