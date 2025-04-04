﻿using System.Collections.Concurrent;

namespace Net.Mqtt.Tests.BitSetIdentifierPool;

[TestClass]
[DoNotParallelize]
public class RentShould
{
    private readonly ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 8 };

    [TestMethod]
    public void ThrowInvalidOperationExceptionWhenExceedPoolLimits()
    {
        const int rents = 65536;
        var pool = new Mqtt.BitSetIdentifierPool();
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            try
            {
                Parallel.For(0, rents, parallelOptions, _ => pool.Rent());
            }
            catch (AggregateException exception)
            {
                throw exception.GetBaseException();
            }
        });
    }

    [TestMethod]
    public void ReturnDistinctSequenceSingleThread()
    {
        const int rents = 2048;
        var pool = new Mqtt.BitSetIdentifierPool();
        var list = new List<ushort>(rents);

        for (var i = 0; i < rents; i++) list.Add(pool.Rent());

        Assert.AreEqual(rents, list.Distinct().Count());
    }

    [TestMethod]
    public void ReturnDistinctSequenceMultiThread()
    {
        var bag = new ConcurrentBag<ushort>();
        var pool = new Mqtt.BitSetIdentifierPool();

        Parallel.For(0, 65535, parallelOptions, _ => bag.Add(pool.Rent()));

        Assert.AreEqual(65535, bag.Distinct().Count());
    }
}