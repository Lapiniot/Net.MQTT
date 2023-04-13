using System.Net.Mqtt.Server.Protocol.V3;

namespace System.Net.Mqtt.Server.Protocol.V5;

public class MqttServerSession5 : MqttServerSession3
{
    public MqttServerSession5(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, Observers observers, int maxUnflushedBytes) :
        base(clientId, transport, stateRepository, logger, observers, maxUnflushedBytes)
    {
    }
}