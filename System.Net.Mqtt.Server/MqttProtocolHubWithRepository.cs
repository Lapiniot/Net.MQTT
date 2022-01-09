﻿using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server.Exceptions;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;
using static System.Net.Mqtt.Packets.ConnAckPacket;
using static System.String;

namespace System.Net.Mqtt.Server;

public abstract partial class MqttProtocolHubWithRepository<T> : MqttProtocolHub, ISessionStateRepository<T>, IAsyncDisposable
    where T : MqttServerSessionState
{
    private readonly ILogger logger;  
    private readonly IMqttAuthenticationHandler authHandler;
    private readonly ConcurrentDictionary<string, T> states;
    private readonly ChannelReader<Message> messageQueueReader;
    private readonly ChannelWriter<Message> messageQueueWriter;
    private readonly CancelableOperationScope messageWorker;
    private readonly int maxDop;
    private readonly int parallelDispatchThreshold;
    private int disposed;

    protected ILogger Logger => logger;

    protected MqttProtocolHubWithRepository(ILogger logger, IMqttAuthenticationHandler authHandler, int maxDop = -1, int parallelDispatchThreshold = 8)
    {  
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
        this.authHandler = authHandler;
        this.maxDop = maxDop;
        this.parallelDispatchThreshold = parallelDispatchThreshold;

        states = new ConcurrentDictionary<string, T>();
        (messageQueueReader, messageQueueWriter) = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions() { SingleReader = false, SingleWriter = false });
        messageWorker = CancelableOperationScope.StartInScope(ProcessMessageQueueAsync);
    }

    #region Overrides of MqttProtocolHub

    public override async Task<MqttServerSession> AcceptConnectionAsync(NetworkTransport transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport);

        var reader = transport.Reader;

        var rvt = MqttPacketHelpers.ReadPacketAsync(reader, cancellationToken);
        var (_, _, _, buffer) = rvt.IsCompletedSuccessfully ? rvt.Result : await rvt.ConfigureAwait(false);

        try
        {
            if(ConnectPacket.TryRead(in buffer, out var connPack, out _))
            {
                var vvt = ValidateAsync(transport, connPack, cancellationToken);
                if(!vvt.IsCompletedSuccessfully)
                {
                    await vvt.ConfigureAwait(false);
                }

                if(authHandler?.Authenticate(connPack.UserName, connPack.Password) == false)
                {
                    await transport.SendAsync(new byte[] { 0b0010_0000, 2, 0, NotAuthorized }, cancellationToken).ConfigureAwait(false);
                    throw new AuthenticationException();
                }

                Message? willMessage = !IsNullOrEmpty(connPack.WillTopic)
                    ? new(connPack.WillTopic, connPack.WillMessage, connPack.WillQoS, connPack.WillRetain)
                    : null;

                return CreateSession(connPack, willMessage, transport, subscribeObserver, messageObserver);
            }
            else
            {
                throw new MissingConnectPacketException();
            }
        }
        finally
        {
            reader.AdvanceTo(buffer.End);
        }
    }

    protected abstract ValueTask ValidateAsync(NetworkTransport transport, ConnectPacket connectPacket, CancellationToken cancellationToken);

    protected abstract MqttServerSession CreateSession(ConnectPacket connectPacket, Message? willMessage, NetworkTransport transport,
        IObserver<SubscriptionRequest> subscribeObserver, IObserver<IncomingMessage> messageObserver);

    public override void DispatchMessage(Message message)
    {
        if(states.IsEmpty) return;
        messageQueueWriter.TryWrite(message);
    }

    #endregion

    private async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = maxDop,
                TaskScheduler = TaskScheduler.Default,
                CancellationToken = cancellationToken
            };

            while(!cancellationToken.IsCancellationRequested)
            {
                var vt = messageQueueReader.ReadAsync(cancellationToken);
                var message = vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);

                if(states.IsEmpty) { continue; }

                var dispatchState = ObjectPool<MessageDispatchState>.Shared.Rent();

                try
                {
                    dispatchState.Message = message;
                    dispatchState.Logger = logger;

                    if(states.TryGetNonEnumeratedCount(out var count) && count < parallelDispatchThreshold)
                    {
                        foreach(var (_, state) in states)
                        {
                            dispatchState.Dispatch(state);
                        }
                    }
                    else
                    {
                        Parallel.ForEach(states.Values, options, dispatchState.Dispatch);
                    }
                }
                finally
                {
                    ObjectPool<MessageDispatchState>.Shared.Return(dispatchState);
                }
            }
        }
        catch(OperationCanceledException)
        {
            // expected
        }
        catch(ChannelClosedException)
        {
            // expected
        }
    }

    #region Implementation of ISessionStateRepository<out T>

    public T GetOrCreate(string clientId, bool cleanSession, out bool existingSession)
    {
        var existing = false;
        var state = states.AddOrUpdate(clientId, CreateState, (id, old, clean) =>
        {
            if(!clean)
            {
                existing = true;
                return old;
            }

            (old as IDisposable)?.Dispose();
            return CreateState(id, true);
        }, cleanSession);

        existingSession = existing;
        return state;
    }

    protected abstract T CreateState(string clientId, bool clean);

    public void Remove(string clientId)
    {
        states.TryRemove(clientId, out var state);
        (state as IDisposable)?.Dispose();
    }

    #endregion

    [LoggerMessage(17, LogLevel.Debug, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage")]
    protected partial void LogOutgoingMessage(string clientId, string topic, int size, byte qos, bool retain);

    #region Implementation of IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if(Interlocked.Exchange(ref disposed, 1) != 0) { return; }

        GC.SuppressFinalize(this);

        try
        {
            messageQueueWriter.Complete();
            await messageWorker.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Parallel.ForEach(states.Values, state => (state as IDisposable)?.Dispose());
        }
    }

    #endregion
}