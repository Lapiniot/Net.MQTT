using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.FastPacketIdPool;

[TestClass]
[DoNotParallelize]
public class RentShould
{
    private readonly ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };

    [TestMethod]
    public void ThrowInvalidOperationExceptionWhenExceedPoolLimits()
    {
        const int rents = 65536;
        var pool = new Mqtt.FastPacketIdPool();
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            try
            {
                Parallel.For(0, rents, parallelOptions, _ => pool.Rent());
            }
            catch(AggregateException exception)
            {
                throw exception.GetBaseException();
            }
        });
    }

    [TestMethod]
    public void ReturnDistinctSequenceSingleThread()
    {
        const int rents = 2048;
        var pool = new Mqtt.FastPacketIdPool();
        var list = new List<ushort>(rents);

        for(var i = 0; i < rents; i++) list.Add(pool.Rent());

        Assert.AreEqual(rents, list.Distinct().Count());
    }

    [TestMethod]
    public void ReturnDistinctSequenceMultiThread()
    {
        var bag = new ConcurrentBag<ushort>();
        var pool = new Mqtt.FastPacketIdPool();

        Parallel.For(0, 65535, parallelOptions, _ => bag.Add(pool.Rent()));

        Assert.AreEqual(65535, bag.Distinct().Count());
    }

    /*
    private readonly ConcurrentDictionary<ushort, bool> map = new ConcurrentDictionary<ushort, bool>();

    [TestMethod]
    public void ReturnDistinctSequence_MultiThreadCD()
    {
        var bag = new ConcurrentBag<ushort>();

        Parallel.For(0, 65536, parallelOptions, _ => bag.Add(Rent()));

        Assert.AreEqual(65536, bag.Distinct().Count());
    }

    private ushort Rent()
    {
        for(ushort i = 0; i <= ushort.MaxValue; i++)
        {
            if(map.TryAdd(i, true)) return i;
        }

        throw new ArgumentException();
    }
    */
}