using System.Net.Mqtt.Packets.V5;

namespace System.Net.Mqtt.Server.Protocol.V5;

public sealed partial class MqttServerSession5 : MqttServerSession
{
    private readonly ISessionStateRepository<MqttServerSessionState5> stateRepository;
#pragma warning disable CA2213
    private MqttServerSessionState5? state;
#pragma warning restore CA2213

    public bool CleanStart { get; init; }
    public uint ExpiryInterval { get; init; }
    public Message5? WillMessage { get; init; }
    public uint WillDelayInterval { get; init; }
    public bool HasAssignedClientId { get; init; }

    public MqttServerSession5(string clientId, NetworkTransportPipe transport,
        ISessionStateRepository<MqttServerSessionState5> stateRepository,
        ILogger logger, int maxUnflushedBytes) :
        base(clientId, transport, logger, true)
    {
        this.maxUnflushedBytes = maxUnflushedBytes;
        this.stateRepository = stateRepository;
        clientAliases = new();
        serverAliases = new(ByteSequenceComparer.Instance);
        nextTopicAlias = 1;
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = stateRepository.Acquire(ClientId, CleanStart, out var exists);

        new ConnAckPacket(ConnAckPacket.Accepted, exists)
        {
            RetainAvailable = false,
            SharedSubscriptionAvailable = false,
            TopicAliasMaximum = ServerTopicAliasMaximum,
            AssignedClientId = HasAssignedClientId ? UTF8.GetBytes(ClientId) : ReadOnlyMemory<byte>.Empty,
        }.Write(Transport.Output, out _);
        await Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

        state.SetWillMessageState(WillMessage, IncomingObserver);

        (reader, writer) = Channel.CreateUnbounded<PacketDispatchBlock>(new() { SingleReader = true, SingleWriter = false });
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        state.IsActive = true;

        if (exists)
        {
            state.DispatchPendingMessages(resendPublishHandler ??= ResendPublish);
        }
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            writer!.TryComplete();
            Transport.Output.CancelPendingFlush();

            state!.PublishWillMessage(TimeSpan.FromSeconds(WillDelayInterval));

            await base.StoppingAsync().ConfigureAwait(false);
        }
        finally
        {
            if (ExpiryInterval is 0)
            {
                stateRepository.Discard(ClientId);
            }
            else
            {
                stateRepository.Release(ClientId, ExpiryInterval is uint.MaxValue ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(ExpiryInterval));
            }
        }
    }

    protected override async Task WaitCompletedAsync()
    {
        try
        {
            await base.WaitCompletedAsync().ConfigureAwait(false);
        }
        catch (InvalidTopicAliasException)
        {
            Disconnect(DisconnectReason.TopicAliasInvalid);
        }
        finally
        {
            Abort();
            try
            {
                // Ensure outgoing data stream producer is done, 
                // so there is no interference with direct Transport.Output writing operation
                await ProducerCompletion.ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                // expected, don't throw
            }

            if (!DisconnectReceived && DisconnectReason is not DisconnectReason.Normal)
            {
                await SendDisconnectAsync((byte)DisconnectReason).ConfigureAwait(false);
            }
        }

        async Task SendDisconnectAsync(byte reasonCode)
        {
            new DisconnectPacket(reasonCode).Write(Transport.Output, out _);
            await Transport.Output.CompleteAsync().ConfigureAwait(false);
            await Transport.OutputCompletion.ConfigureAwait(false);
        }
    }

    protected override void OnPacketReceived(byte packetType, int totalLength) => DisconnectPending = false;

    protected override void OnPacketSent(byte packetType, int totalLength) { }
}