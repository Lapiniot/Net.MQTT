using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server;

public abstract class MqttServerSession : MqttServerProtocol
{
    private readonly IObserver<IncomingMessage> messageObserver;

    protected MqttServerSession(string clientId, NetworkTransport transport,
        ILogger logger, IObserver<IncomingMessage> messageObserver, bool disposeTransport) :
        base(transport, disposeTransport)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        ClientId = clientId;
        Logger = logger;
        this.messageObserver = messageObserver;
    }

    protected ILogger Logger { get; }
    public string ClientId { get; init; }
    public bool DisconnectReceived { get; protected set; }

    protected void OnMessageReceived(Message message)
    {
        messageObserver.OnNext(new IncomingMessage(message, ClientId));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return StartActivityAsync(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task StopAsync()
    {
        return StopActivityAsync();
    }

    public override string ToString()
    {
        return $"'{ClientId}' over '{Transport}'";
    }

    public async Task WaitCompletedAsync(CancellationToken cancellationToken)
    {
        await Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        await Transport.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}