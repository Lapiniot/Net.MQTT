﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.FastPacketIdPool
{
    [TestClass]
    [DoNotParallelize]
    public class ReturnShould
    {
        private readonly ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };

        [TestMethod]
        [SuppressMessage("Security", "CA5394: Do not use insecure randomness")]
        public void ReturnSelectedItemsToThePool()
        {
            var pool = new Mqtt.FastPacketIdPool();

            // Allocate all items from the pull
            Parallel.For(0, 65535, parallelOptions, _ => pool.Rent());
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                try
                {
                    pool.Rent();
                }
                catch(AggregateException e)
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
            Parallel.ForEach(ids, parallelOptions, id => pool.Release(id));

            // Act: try to rent the same quantity of ids from the pool
            Parallel.ForEach(ids, parallelOptions, _ => bag.Add(pool.Rent()));

            // Expected: items returned to the pool should become available to rent again
            Assert.IsTrue(ids.SequenceEqual(bag.OrderBy(t => t)));
        }

        [TestMethod]
        public void ThrowInvalidArgumentExceptionWhenReturnNotTrackedItemOutOfList()
        {
            var pool = new Mqtt.FastPacketIdPool();
            Parallel.For(0, 64, parallelOptions, _ => pool.Rent());
            Assert.ThrowsException<InvalidOperationException>(() => pool.Release(100));
        }

        [TestMethod]
        public void ThrowInvalidArgumentExceptionWhenReturnNotTrackedItem()
        {
            var pool = new Mqtt.FastPacketIdPool();
            Parallel.For(0, 64, parallelOptions, _ => pool.Rent());
            pool.Release(33);
            Assert.ThrowsException<InvalidOperationException>(() => pool.Release(33));
        }
    }
}