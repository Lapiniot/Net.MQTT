namespace System.Net.Mqtt.Benchmarks.Base32;

public static class Base32V2
{
    public static string ToBase32String(long value) => string.Create(13, value, WriteBuffer);

    [MethodImpl(NoInlining)]
    private static void WriteBuffer(Span<char> span, long value)
    {
        // this fixed size array will be referenced directly from 
        // data section of dll as a part of no-alloc optimization
        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8;

        // write in backward order to eliminate redundant bounds checks
        span[12] = (char)alphabet[(int)(value & 31)];
        span[11] = (char)alphabet[(int)((value >>> 5) & 31)];
        span[10] = (char)alphabet[(int)((value >>> 10) & 31)];
        span[9] = (char)alphabet[(int)((value >>> 15) & 31)];
        span[8] = (char)alphabet[(int)((value >>> 20) & 31)];
        span[7] = (char)alphabet[(int)((value >>> 25) & 31)];
        span[6] = (char)alphabet[(int)((value >>> 30) & 31)];
        span[5] = (char)alphabet[(int)((value >>> 35) & 31)];
        span[4] = (char)alphabet[(int)((value >>> 40) & 31)];
        span[3] = (char)alphabet[(int)((value >>> 45) & 31)];
        span[2] = (char)alphabet[(int)((value >>> 50) & 31)];
        span[1] = (char)alphabet[(int)((value >>> 55) & 31)];
        span[0] = (char)alphabet[(int)((value >>> 60) & 31)];
    }
}