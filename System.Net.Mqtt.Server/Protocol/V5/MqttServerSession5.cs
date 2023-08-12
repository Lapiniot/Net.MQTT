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
        ILogger logger, int maxUnflushedBytes, ushort maxInFlight) :
        base(clientId, transport, logger, true)
    {
        Verify.ThrowIfLess(maxInFlight, 1);
        this.maxUnflushedBytes = maxUnflushedBytes;
        this.stateRepository = stateRepository;
        clientAliases = [];
        serverAliases = new(ByteSequenceComparer.Instance);
        nextTopicAlias = 1;
        inflightSentinel = new(maxInFlight, maxInFlight);
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        state = stateRepository.Acquire(ClientId, CleanStart, out var exists);

        new ConnAckPacket(ConnAckPacket.Accepted, exists)
        {
            RetainAvailable = false,
            SharedSubscriptionAvailable = false,
            TopicAliasMaximum = ServerTopicAliasMaximum,
            ReceiveMaximum = ReceiveMaximum,
            MaximumPacketSize = (uint)MaxReceivePacketSize,
            AssignedClientId = HasAssignedClientId ? UTF8.GetBytes(ClientId) : ReadOnlyMemory<byte>.Empty,
        }.Write(Transport.Output, int.MaxValue, out _);
        await Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

        state.SetWillMessageState(WillMessage, IncomingObserver);
        (reader, writer) = Channel.CreateUnbounded<PacketDispatchBlock>(new() { SingleReader = true, SingleWriter = false });
        receivedIncompleteQoS2 = 0;

        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        state.IsActive = true;
    }

    protected override async Task StoppingAsync()
    {
        try
        {
            state!.PublishWillMessage(TimeSpan.FromSeconds(WillDelayInterval));

            await base.StoppingAsync().ConfigureAwait(false);
        }
        catch (InvalidTopicAliasException) { }
        catch (ReceiveMaximumExceededException) { }
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

    protected override async Task ExecuteAsync()
    {
        try
        {
            await base.ExecuteAsync().ConfigureAwait(false);
        }
        catch (InvalidTopicAliasException)
        {
            Disconnect(DisconnectReason.TopicAliasInvalid);
        }
        catch (ReceiveMaximumExceededException)
        {
            Disconnect(DisconnectReason.ReceiveMaximumExceeded);
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
            new DisconnectPacket(reasonCode).Write(Transport.Output, int.MaxValue, out _);
            await Transport.Output.CompleteAsync().ConfigureAwait(false);
            await Transport.OutputCompletion.ConfigureAwait(false);
        }
    }

    protected override void OnPacketReceived(byte packetType, int totalLength) => DisconnectPending = false;

    protected override void OnPacketSent(byte packetType, int totalLength) { }
}