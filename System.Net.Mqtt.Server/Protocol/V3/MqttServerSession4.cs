namespace System.Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSession4(string clientId, NetworkTransportPipe transport,
    ISessionStateRepository<MqttServerSessionState4> stateRepository,
    ILogger logger, int maxUnflushedBytes, ushort maxInFlight) :
    MqttServerSession3(clientId, transport, stateRepository, logger, maxUnflushedBytes, maxInFlight)
{ }