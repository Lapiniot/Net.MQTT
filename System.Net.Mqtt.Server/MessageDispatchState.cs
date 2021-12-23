using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server;

internal struct MessageDispatchState
{
    private Message message;
    private ILogger logger;
    private Func<MqttServerSessionState, CancellationToken, ValueTask> dispatchFunc;
    private static readonly Action<ILogger, string, string, int, byte, bool, Exception> logMessage =
        LoggerMessage.Define<string, string, int, byte, bool>(LogLevel.Debug, 17,
            "Outgoing message for '{ClientId}': Topic = '{Topic}', Size = {Size}, QoS = {Qos}, Retain = {Retain}");

    public MessageDispatchState(Message message, ILogger logger)
    {
        this.message = message;
        this.logger = logger;
        dispatchFunc = null;
    }

    public Message Message { get => message; set => message = value; }

    public ILogger Logger { get => logger; set => logger = value; }

    public Func<MqttServerSessionState, CancellationToken, ValueTask> Dispatcher =>
        dispatchFunc ??= new Func<MqttServerSessionState, CancellationToken, ValueTask>(DispatchAsync);

    private ValueTask DispatchAsync(MqttServerSessionState sessionState, CancellationToken cancellationToken)
    {
        var (topic, payload, qos, _) = message;

        if(!sessionState.IsActive && qos == 0)
        {
            // Skip all incoming QoS 0 if session is inactive
            return ValueTask.CompletedTask;
        }

        if(!sessionState.TopicMatches(topic, out var maxQoS))
        {
            return ValueTask.CompletedTask;
        }

        var adjustedQoS = Math.Min(qos, maxQoS);

        var msg = qos == adjustedQoS ? message : message with { QoSLevel = adjustedQoS };

        logMessage(logger, sessionState.ClientId, topic, payload.Length, adjustedQoS, false, null);

        return sessionState.EnqueueAsync(msg, cancellationToken);
    }
}