using System.Runtime.CompilerServices;

namespace System.Net.Mqtt;

public static class Verify
{
    public static void ThrowIfNotInRange(int argument, int minVal, int maxVal, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (argument < minVal || argument > maxVal)
        {
            throw new ArgumentOutOfRangeException(argumentName, $"Must be number in the range [{minVal} .. {maxVal}]");
        }
    }

    internal static void ThrowIfNullOrEmpty(string argument, [CallerArgumentExpression("argument")] string argumentName = null)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new ArgumentException("Cannot be null or empty.", argumentName);
        }
    }
}