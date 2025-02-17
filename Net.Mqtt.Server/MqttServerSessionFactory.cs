namespace Net.Mqtt.Server;

public abstract class MqttServerSessionFactory
{
    public abstract Task<MqttServerSession> AcceptConnectionAsync(TransportConnection connection, CancellationToken cancellationToken);
}