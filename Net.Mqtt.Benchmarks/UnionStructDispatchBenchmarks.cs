using System.Runtime.InteropServices;

namespace Net.Mqtt.Benchmarks;

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class UnionStructDispatchBenchmarks
{
    private readonly List<DataBlock> data1 = GenerateData(100000);
    private readonly List<DataBlockD> data2 = GenerateDataD(100000);

    private static List<DataBlock> GenerateData(int count)
    {
        var rnd = new Random(37);
        var list = new List<DataBlock>(count);
        var bytes = "test data"u8.ToArray();
        for (var i = 0; i < count; i++)
        {
            switch (rnd.Next(0, 4))
            {
                case 0: list.Add(new() { Value = 1, Data = default }); break;
                case 1: list.Add(new() { Value = default, Data = new DispatchData() { Value = 4 } }); break;
                case 2: list.Add(new() { Value = default, Data = new DispatchDataConcrete() { Value = 4 } }); break;
                case 3: list.Add(new() { Value = default, Data = default, Buffer = bytes }); break;
            }
        }

        return list;
    }

    private static List<DataBlockD> GenerateDataD(int count)
    {
        var rnd = new Random(37);
        var list = new List<DataBlockD>(count);
        var bytes = "test data"u8.ToArray();
        for (var i = 0; i < count; i++)
        {
            switch (rnd.Next(0, 4))
            {
                case 0: list.Add(new() { D = 0, Value = 1 }); break;
                case 1: list.Add(new() { D = 1, Data = new DispatchData() { Value = 4 } }); break;
                case 2: list.Add(new() { D = 2, Data = new DispatchDataConcrete() { Value = 4 } }); break;
                case 3: list.Add(new() { D = 3, Buffer = bytes }); break;
            }
        }

        return list;
    }

    [Benchmark(Baseline = true)]
    public void DispatchByType()
    {
        int dataObjectConcreteCount = 0, dataObjectsCount = 0, intsCount = 0, buffersCount = 0;
        for (var i = 0; i < data1.Count; i++)
        {
            var d = data1[i];
            if (d.Data is DispatchDataConcrete)
            {
                dataObjectConcreteCount++;
            }
            else if (d.Data is not null)
            {
                dataObjectsCount++;
            }
            else if (!d.Buffer.IsEmpty)
            {
                buffersCount++;
            }
            else if (d.Value is not 0)
            {
                intsCount++;
            }
        }

        // Console.WriteLine("DispatchByType: dataObjectConcreteCount={0}, dataObjectsCount={1}, intsCount={2}, buffersCount={3}",
        //     dataObjectConcreteCount.ToString(CultureInfo.InvariantCulture), dataObjectsCount.ToString(CultureInfo.InvariantCulture),
        //     intsCount.ToString(CultureInfo.InvariantCulture), buffersCount.ToString(CultureInfo.InvariantCulture));
    }

    [Benchmark]
    public void DispatchByDiscriminant()
    {
        int dataObjectConcreteCount = 0, dataObjectsCount = 0, intsCount = 0, buffersCount = 0;
        for (var i = 0; i < data2.Count; i++)
        {
            var d = data2[i];
            switch (d.D)
            {
                case 0:
                    intsCount++;
                    break;
                case 1:
                    dataObjectsCount++;
                    break;
                case 2:
                    dataObjectConcreteCount++;
                    break;
                case 3:
                    buffersCount++;
                    break;
                default:
                    break;
            }
        }

        // Console.WriteLine("DispatchByDiscriminant: dataObjectConcreteCount={0}, dataObjectsCount={1}, intsCount={2}, buffersCount={3}",
        //     dataObjectConcreteCount.ToString(CultureInfo.InvariantCulture), dataObjectsCount.ToString(CultureInfo.InvariantCulture),
        //     intsCount.ToString(CultureInfo.InvariantCulture), buffersCount.ToString(CultureInfo.InvariantCulture));
    }
}

internal struct DataBlock
{
    public int Value;
    public IDispatchData Data;
    public ReadOnlyMemory<byte> Buffer;
}

[StructLayout(LayoutKind.Explicit)]
internal record struct DataBlockD
{
    [FieldOffset(0)]
    public uint D;
    [FieldOffset(4)]
    public int Value;
    [FieldOffset(8)]
    public IDispatchData Data;
    [FieldOffset(8)]
    public ReadOnlyMemory<byte> Buffer;
}

public interface IDispatchData { int Value { get; set; } }

public class DispatchData : IDispatchData
{
    public int Value { get; set; }
}

public sealed class DispatchDataConcrete : IDispatchData
{
    public int Value { get; set; }
}