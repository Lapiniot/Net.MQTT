namespace System.Net.Mqtt.Client;

public class MqttClientSessionState : MqttSessionState
{
    public MqttClientSessionState(int maxInflight) : base(maxInflight)
    { }

    #region Overrides of MqttSessionState

    /// <inheritdoc />
    public sealed override Task<ushort> CreateMessageDeliveryStateAsync(byte flags, Utf8String topic, Utf8String payload, CancellationToken cancellationToken) =>
        base.CreateMessageDeliveryStateAsync(flags, topic, payload, cancellationToken);

    /// <inheritdoc />
    public sealed override bool DiscardMessageDeliveryState(ushort packetId) => base.DiscardMessageDeliveryState(packetId);

    #endregion
}