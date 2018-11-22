namespace System.Net.Mqtt.Server
{
    public interface ISessionStateProvider<out T>
    {
        void Remove(string clientId);
        T GetOrCreate(string clientId, bool clean);
    }
}