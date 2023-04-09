namespace System.Net.Mqtt.Benchmarks.Base32;

public static class Base32V1
{
    private static readonly char[] Alphabet =
    {
        '0', '1', '2', '3', '4', '5', '6', '7',
        '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
        'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
        'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V'
    };

    [MethodImpl(AggressiveInlining)]
    public static string ToBase32String(long value) => string.Create(13, value, WriteBuffer);

    private static void WriteBuffer(Span<char> span, long value)
    {
        span[12] = Alphabet[(int)(value & 31)];
        span[11] = Alphabet[(int)(value >> 5 & 31)];
        span[10] = Alphabet[(int)(value >> 10 & 31)];
        span[9] = Alphabet[(int)(value >> 15 & 31)];
        span[8] = Alphabet[(int)(value >> 20 & 31)];
        span[7] = Alphabet[(int)(value >> 25 & 31)];
        span[6] = Alphabet[(int)(value >> 30 & 31)];
        span[5] = Alphabet[(int)(value >> 35 & 31)];
        span[4] = Alphabet[(int)(value >> 40 & 31)];
        span[3] = Alphabet[(int)(value >> 45 & 31)];
        span[2] = Alphabet[(int)(value >> 50 & 31)];
        span[1] = Alphabet[(int)(value >> 55 & 31)];
        span[0] = Alphabet[(int)(value >> 60 & 31)];
    }
}