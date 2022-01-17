using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server;

public partial class MessageDispatchState
{
    private Message message;
    private ILogger logger;
    private readonly Action<MqttServerSessionState> dispatchFunc;

    public MessageDispatchState()
    {
        dispatchFunc = new(DispatchInternal);
    }

    public Message Message { get => message; set => message = value; }

    public ILogger Logger { get => logger; set => logger = value; }

    public Action<MqttServerSessionState> Dispatch => dispatchFunc;

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

        LogOutgoingMessage(sessionState.ClientId, topic, payload.Length, adjustedQoS, false);

        sessionState.TryEnqueue(qos == adjustedQoS
            ? message
            : message with { QoSLevel = adjustedQoS });
    }

    [LoggerMessage(17, LogLevel.Debug, "Outgoing message for '{clientId}': Topic = '{topic}', Size = {size}, QoS = {qos}, Retain = {retain}", EventName = "OutgoingMessage")]
    private partial void LogOutgoingMessage(string clientId, string topic, int size, byte qos, bool retain);
}