namespace System.Net.Mqtt.Server
{
    public interface ISessionStateProvider<out T>
    {
        T Get(string clientId);
        void Clear(string clientId);
    }
}