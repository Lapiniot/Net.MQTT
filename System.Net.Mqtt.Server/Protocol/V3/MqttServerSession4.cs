namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSession4 : MqttServerSession3
{
    public MqttServerSession4(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState4> stateRepository,
        ILogger logger, int maxUnflushedBytes, int maxInFlight) :
        base(clientId, transport, stateRepository, logger, maxUnflushedBytes, maxInFlight)
    { }
}