namespace System.Net.Mqtt.Server
{
    public interface ISessionStateProvider<out T>
    {
        T Get(string clientId);
        T Remove(string clientId);
        T Create(string clientId, bool persistent);
    }
}