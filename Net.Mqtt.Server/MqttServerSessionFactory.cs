namespace Net.Mqtt.Server;

public abstract class MqttServerSessionFactory
{
    public abstract Task<MqttServerSession> AcceptConnectionAsync(NetworkTransportPipe transport, CancellationToken cancellationToken);
}