namespace System.Net.Mqtt;

public static class MqttHelpers
{
    [MethodImpl(AggressiveInlining)]
    public static int GetVarBytesCount(uint value) => BitOperations.Log2(value) / 7 + 1;

    [MethodImpl(AggressiveInlining)]
    public static int GetUserPropertiesSize(IReadOnlyList<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)> properties)
    {
        if (properties is null)
            return 0;

        var total = 0;
        for (var i = 0; i < properties.Count; i++)
        {
            var pair = properties[i];
            total += 5 + pair.Item1.Length + pair.Item2.Length;
        }

        return total;
    }
}