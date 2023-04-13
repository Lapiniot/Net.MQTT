using SequenceExtensions = System.Net.Mqtt.Extensions.SequenceExtensions;

namespace System.Net.Mqtt.Server.Protocol.V5;

public class ProtocolHub5 : MqttProtocolHub
{
    private readonly ILogger logger;
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly int maxInFlight;
    private readonly int maxUnflushedBytes;

    public ProtocolHub5(ILogger logger, IMqttAuthenticationHandler authHandler, int maxInFlight, int maxUnflushedBytes)
    {
        this.logger = logger;
        this.authHandler = authHandler;
        this.maxInFlight = maxInFlight;
        this.maxUnflushedBytes = maxUnflushedBytes;
    }

    public override int ProtocolLevel => 5;

    public override async Task<MqttServerSession> AcceptConnectionAsync([NotNull] NetworkTransportPipe transport, Observers observers, CancellationToken cancellationToken)
    {
        var reader = transport.Input;

        var packet = await MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);
        var buffer = packet.Buffer;

        SequenceExtensions.DebugDump(buffer);

        throw new NotImplementedException();
    }

    public override void DispatchMessage(Message message) => throw new NotImplementedException();
}