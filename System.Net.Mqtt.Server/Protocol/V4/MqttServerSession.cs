namespace System.Net.Mqtt.Server.Protocol.V4;

public class MqttServerSession : V3.MqttServerSession
{
    public MqttServerSession(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<V3.MqttServerSessionState> stateRepository, ILogger logger,
        Observers observers, int maxUnflushedBytes) :
        base(clientId, transport, stateRepository, logger, observers, maxUnflushedBytes)
    { }
}