namespace System.Net.Mqtt.Server.Protocol.V4;

public class MqttServerSession : V3.MqttServerSession
{
    public MqttServerSession(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<V3.MqttServerSessionState> stateRepository, ILogger logger,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver, int maxUnflushedBytes) :
        base(clientId, transport, stateRepository, logger, subscribeObserver, messageObserver, maxUnflushedBytes)
    { }

    #region Overrides of ServerSession

    protected override async ValueTask AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
    {
        try
        {
            await Transport.Output.WriteAsync(new byte[] { 0b0010_0000, 2, (byte)(existing ? 1 : 0), ConnAckPacket.Accepted }, cancellationToken).ConfigureAwait(false);
        }
        catch (ConnectionClosedException)
        {
            // This type of error is kind of acceptable here, because client may has already 
            // completed its work and disconnected before receiving CONNACK packet as soon 
            // as it is allowed per MQTT v3.1.1 specification
        }
    }

    #endregion
}