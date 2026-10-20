namespace Net.Mqtt.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class MultipleResultsBenchmarks
{
    public int Input { get; set; } = 1000000;

    public ResultKind Kind { get; set; }

    [Benchmark(Baseline = true)]
    public long ValueTuple()
    {
        var sum = 0L;
        for (var i = 0; i < Input; i++)
        {
            var result = GetValueTuple(i, Kind);
            sum += Pack(result.Item1, result.Item2);
        }

        return sum;
    }

    [Benchmark]
    public long OutParameters()
    {
        var sum = 0L;
        for (var i = 0; i < Input; i++)
        {
            GetOutParameters(i, Kind, out var value, out var kind);
            sum += Pack(value, kind);
        }

        return sum;
    }

    [Benchmark]
    public long OutParametersMix()
    {
        var sum = 0L;
        for (var i = 0; i < Input; i++)
        {
            var value = GetOutParametersMix(i, Kind, out var kind);
            sum += Pack(value, kind);
        }

        return sum;
    }

    [Benchmark]
    public long KeyValuePair()
    {
        var sum = 0L;
        for (var i = 0; i < Input; i++)
        {
            var result = GetKeyValuePair(i, Kind);
            sum += Pack(result.Key, result.Value);
        }

        return sum;
    }

    [MethodImpl(NoInlining)]
    private static (int, ResultKind) GetValueTuple(int value, ResultKind kind) => (value, kind);

    [MethodImpl(NoInlining)]
    private static void GetOutParameters(int input, ResultKind resultKind, out int value, out ResultKind kind)
    {
        value = input;
        kind = resultKind;
    }

    [MethodImpl(NoInlining)]
    private static int GetOutParametersMix(int input, ResultKind resultKind, out ResultKind kind)
    {
        kind = resultKind;
        return input;
    }

    [MethodImpl(NoInlining)]
    private static KeyValuePair<int, ResultKind> GetKeyValuePair(int value, ResultKind kind) => new(value, kind);

    private static long Pack(int value, ResultKind kind) => ((long)value << 32) | (uint)kind;

    public enum ResultKind
    {
        None,
        Accepted = 1
    }
}