using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.FastPacketIdPoolTests
{
    [TestClass]
    [DoNotParallelize]
    public class FastPacketIdPool_Rent_Should
    {
        private readonly ParallelOptions parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = 8};

        [TestMethod]
        public void Throw_InvalidOperationException_WhenExceedPoolLimits()
        {
            const int rents = 65536;
            var pool = new FastPacketIdPool();
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
        public void ReturnDistinctSequence_SingleThread()
        {
            const int rents = 2048;
            var pool = new FastPacketIdPool();
            var list = new List<ushort>(rents);

            for(var i = 0; i < rents; i++) list.Add(pool.Rent());

            Assert.AreEqual(rents, list.Distinct().Count());
        }

        [TestMethod]
        public void ReturnDistinctSequence_MultiThread()
        {
            var bag = new ConcurrentBag<ushort>();
            var pool = new FastPacketIdPool();

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
}