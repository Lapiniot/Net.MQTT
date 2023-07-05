namespace System.Net.Mqtt.Server;

/// <summary>
/// Represents base abstract MQTT version specific protocol hub implementation which:
/// 1. Acts as a client session factory for specific version
/// 2. Distributes incoming messages from server across internally maintained list of sessions, according to the specific
/// protocol version rules
/// </summary>
public abstract class MqttProtocolHub<TSender, TMessage> : MqttServerSessionFactory
{
    public abstract int ProtocolLevel { get; }

    public abstract void DispatchMessage(TSender sender, TMessage message);
}