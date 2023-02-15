using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace System.Net.Mqtt.Extensions;

public static partial class MqttExtensions
{
    [MethodImpl(AggressiveInlining)]
    internal static int CommonPrefixLengthScalar(ref byte left, ref byte right, int length)
    {
        nuint i = 0;

        if (length >= 4)
        {
            for (; length >= 4; length -= 4, i += 4)
            {
                if (Unsafe.Add(ref left, i) != Unsafe.Add(ref right, i)) return (int)i;
                if (Unsafe.Add(ref left, i + 1) != Unsafe.Add(ref right, i + 1)) return (int)(i + 1);
                if (Unsafe.Add(ref left, i + 2) != Unsafe.Add(ref right, i + 2)) return (int)(i + 2);
                if (Unsafe.Add(ref left, i + 3) != Unsafe.Add(ref right, i + 3)) return (int)(i + 3);
            }
        }

        for (; length > 0; length--, i++)
        {
            if (Unsafe.Add(ref left, i) != Unsafe.Add(ref right, i)) break;
        }

        return (int)i;
    }

    [MethodImpl(AggressiveInlining)]
    internal static int CommonPrefixLengthSWAR(ref byte left, ref byte right, int length)
    {
        nuint i = 0;

        for (; length >= nuint.Size; length -= nuint.Size, i += (nuint)nuint.Size)
        {
            var x = Unsafe.As<byte, uint>(ref Unsafe.Add(ref left, i)) ^ Unsafe.As<byte, uint>(ref Unsafe.Add(ref right, i));
            if (x == 0) continue;
            return (int)(i + (uint.TrailingZeroCount(x) >> 3));
        }

        for (; length > 0; length--, i++)
        {
            if (Unsafe.Add(ref left, i) != Unsafe.Add(ref right, i)) break;
        }

        return (int)i;
    }
}