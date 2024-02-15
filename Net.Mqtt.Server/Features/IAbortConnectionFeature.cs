namespace Net.Mqtt.Server.Features;

public interface IAbortConnectionFeature
{
    void Abort(string clientId);
}