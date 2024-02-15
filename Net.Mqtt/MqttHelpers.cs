namespace Net.Mqtt;

public static class MqttHelpers
{
    public static int GetVarBytesCount(uint value) => BitOperations.Log2(value) / 7 + 1;

    public static int GetUserPropertiesSize(IReadOnlyList<UserProperty> properties)
    {
        if (properties is null)
            return 0;

        var total = 0;
        var count = properties.Count;
        for (var i = 0; i < count; i++)
        {
            var (name, value) = properties[i];
            total += 5 + name.Length + value.Length;
        }

        return total;
    }

    public static int ComputeAdjustedSizes(int maxSize, int payloadSize,
        ref int propsSize, ref int reasonStringSize, ref int userPropertiesSize, out int remainingLength)
    {
        remainingLength = payloadSize + GetVarBytesCount((uint)propsSize) + propsSize;
        var size = 1 + GetVarBytesCount((uint)remainingLength) + remainingLength;

        if (size <= maxSize)
            // computed total packet size doesn't exceed max allowed bytes limit - 
            // keep all components intact
            return size;

        if (userPropertiesSize is not 0)
        {
            // Try to subtract user properties from packet and reset propertiesLength 
            // to indicate user properties shouldn't be encoded to fit the limit
            propsSize -= userPropertiesSize;
            userPropertiesSize = 0;
            remainingLength = payloadSize + GetVarBytesCount((uint)propsSize) + propsSize;
            size = 1 + GetVarBytesCount((uint)remainingLength) + remainingLength;
        }

        if (size <= maxSize)
            return size;

        if (reasonStringSize is not 0)
        {
            // Try to sacrifice ReasonString property in order to reduce packet size even further
            propsSize -= reasonStringSize;
            reasonStringSize = 0;
            remainingLength = payloadSize + GetVarBytesCount((uint)propsSize) + propsSize;
            size = 1 + GetVarBytesCount((uint)remainingLength) + remainingLength;
        }

        return size;
    }
}