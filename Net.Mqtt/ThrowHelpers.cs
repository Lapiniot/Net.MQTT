using Net.Mqtt.Properties;

namespace Net.Mqtt;

public static class ThrowHelpers
{
    [DoesNotReturn]
    public static void ThrowInvalidPacketId(ushort packetId, [CallerArgumentExpression(nameof(packetId))] string argumentName = null) =>
        throw new ArgumentException(Strings.InvalidPacketId, argumentName);

    [DoesNotReturn]
    public static void ThrowInvalidDispatchBlock() =>
        throw new InvalidOperationException(Strings.InvalidDispatchBlock);

    [DoesNotReturn]
    public static void ThrowCannotWriteToQueue() =>
        throw new InvalidOperationException(Strings.CannotAddOutgoingPacket);
}