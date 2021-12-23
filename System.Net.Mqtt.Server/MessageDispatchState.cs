using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server;

internal struct MessageDispatchState
{
    private Message message;
    private ILogger logger;
    private Action<MqttServerSessionState> dispatchFunc;
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

    public Action<MqttServerSessionState> Dispatch => dispatchFunc ??= new(DispatchInternal);

    private void DispatchInternal(MqttServerSessionState sessionState)
    {
        var (topic, payload, qos, _) = message;

        if(!sessionState.IsActive && qos == 0)
        {
            // Skip all incoming QoS 0 if session is inactive
            return;
        }

        if(!sessionState.TopicMatches(topic, out var maxQoS))
        {
            return;
        }

        var adjustedQoS = Math.Min(qos, maxQoS);

        var msg = qos == adjustedQoS ? message : message with { QoSLevel = adjustedQoS };

        logMessage(logger, sessionState.ClientId, topic, payload.Length, adjustedQoS, false, null);

        sessionState.TryEnqueue(message);
    }
}