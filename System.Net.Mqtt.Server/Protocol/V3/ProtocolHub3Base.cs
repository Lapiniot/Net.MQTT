using System.Net.Mqtt.Packets.V3;

namespace System.Net.Mqtt.Server.Protocol.V3;

public abstract class ProtocolHub3Base<TSessionState> : MqttProtocolHubWithRepository<TSessionState, ConnectPacket>
    where TSessionState : MqttServerSessionState
{
    private readonly IMqttAuthenticationHandler authHandler;

    protected ProtocolHub3Base(ILogger logger, IMqttAuthenticationHandler authHandler) : base(logger) => this.authHandler = authHandler;

    protected sealed override bool Authenticate([NotNull] ConnectPacket connPacket) =>
        authHandler is null || authHandler.Authenticate(UTF8.GetString(connPacket.UserName.Span), UTF8.GetString(connPacket.Password.Span));
}