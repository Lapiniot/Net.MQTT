namespace Net.Mqtt.Server.Protocol.V3;

public sealed class MqttServerSession4(string clientId, NetworkTransportPipe transport,
    ISessionStateRepository<MqttServerSessionState4> stateRepository,
    ILogger logger, int maxUnflushedBytes, ushort maxInFlight, int maxReceivePacketSize) :
    MqttServerSession3(clientId, transport, stateRepository, logger, maxUnflushedBytes, maxInFlight, maxReceivePacketSize)
{
    public override string ToString() => $"'{ClientId}' over '{Transport}' (MQTT3.1.1)";
}