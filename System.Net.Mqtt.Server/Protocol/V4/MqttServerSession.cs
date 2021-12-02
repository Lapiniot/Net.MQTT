using System.Net.Connections.Exceptions;
using Microsoft.Extensions.Logging;
using static System.Net.Mqtt.Packets.ConnAckPacket;

namespace System.Net.Mqtt.Server.Protocol.V4;

public class MqttServerSession : V3.MqttServerSession
{
    public MqttServerSession(string clientId, NetworkTransport transport,
        ISessionStateRepository<V3.MqttServerSessionState> stateRepository, ILogger logger,
        IObserver<SubscriptionRequest> subscribeObserver,
        IObserver<MessageRequest> messageObserver) :
        base(clientId, transport, stateRepository, logger, subscribeObserver, messageObserver)
    {
    }

    #region Overrides of ServerSession

    protected override async ValueTask AcknowledgeConnection(bool existing, CancellationToken cancellationToken)
    {
        try
        {
            var valueTask = Transport.SendAsync(new byte[] { 0b0010_0000, 2, (byte)(existing ? 1 : 0), Accepted }, cancellationToken);
            if(!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.ConfigureAwait(false);
            }
        }
        catch(ConnectionAbortedException)
        {
            // This type of error is kind of exceptable here, because client may has already 
            // completed its work and disconnected before receiving CONNACK packet as soon 
            // as it is allowed per MQTT v3.1 specification
        }
    }

    #endregion
}