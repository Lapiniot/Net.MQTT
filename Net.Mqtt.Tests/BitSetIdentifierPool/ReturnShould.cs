using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.BitSetIdentifierPool;

#pragma warning disable CA5394 //Random is an insecure random number generator. Use cryptographically secure random number generators when randomness is required for security.

[TestClass]
[DoNotParallelize]
public class ReturnShould
{
    private readonly ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 8 };

    [TestMethod]
    public void ReturnSelectedItemsToThePool()
    {
        var pool = new Mqtt.BitSetIdentifierPool();

        // Allocate all items from the pull
        Parallel.For(0, 65535, parallelOptions, _ => pool.Rent());
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            try
            {
                pool.Rent();
            }
            catch (AggregateException e)
            {
                throw e.GetBaseException();
            }
        });

        // Generate random list of distinct ids to be returned to the pool
        var bag = new ConcurrentBag<ushort>();
        var rnd = new Random();
        Parallel.For(0, 100, parallelOptions, _ => bag.Add((ushort)rnd.Next(1, 0xffff)));
        var ids = bag.Distinct().OrderBy(t => t).ToArray();
        bag.Clear();

        // Act: return selected ids to the pool
        Parallel.ForEach(ids, parallelOptions, id => pool.Return(id));

        // Act: try to rent the same quantity of ids from the pool
        Parallel.ForEach(ids, parallelOptions, _ => bag.Add(pool.Rent()));

        // Expected: items returned to the pool should become available to rent again
        Assert.IsTrue(ids.SequenceEqual(bag.OrderBy(t => t)));
    }

    [TestMethod]
    public void ThrowInvalidArgumentExceptionWhenReturnIdZero()
    {
        var pool = new Mqtt.BitSetIdentifierPool();
        Assert.ThrowsException<InvalidOperationException>(() => pool.Return(0));
    }

    [TestMethod]
    public void ThrowInvalidArgumentExceptionWhenReturnNotTrackedItemOutOfList()
    {
        var pool = new Mqtt.BitSetIdentifierPool();
        Parallel.For(0, 64, parallelOptions, _ => pool.Rent());
        Assert.ThrowsException<InvalidOperationException>(() => pool.Return(100));
    }

    [TestMethod]
    public void ThrowInvalidArgumentExceptionWhenReturnNotTrackedItem()
    {
        var pool = new Mqtt.BitSetIdentifierPool();
        Parallel.For(0, 64, parallelOptions, _ => pool.Rent());
        pool.Return(33);
        Assert.ThrowsException<InvalidOperationException>(() => pool.Return(33));
    }
}