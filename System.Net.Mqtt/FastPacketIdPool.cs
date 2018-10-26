using System.Threading;
using static System.Net.Mqtt.Properties.Resources;
using static System.UInt16;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Implements fast concurrent non-blocking id pool, which uses contiguous array
    /// and direct indexing to maintain state
    /// <remarks>
    /// Fast non-blocking synchronization is provided at a cost of bigger memory consumption,
    /// as soon as 65536*4 bytes of state data per instance is uses to store state
    /// </remarks>
    /// </summary>
    public class FastPacketIdPool : IPacketIdPool
    {
        private readonly ushort max;
        private readonly ushort min;
        private readonly int[] pool;

        public FastPacketIdPool(ushort minValue = 1, ushort maxValue = MaxValue)
        {
            if(maxValue < minValue)
            {
                throw new ArgumentException(string.Format(MustBeGreaterMessageFormat, nameof(maxValue),
                    nameof(minValue)));
            }

            max = maxValue;
            min = minValue;
            // TODO: use on-demand growing list of array segments instead of solid array as memory size optimization
            pool = new int[maxValue - minValue + 1];
        }

        public ushort Rent()
        {
            var index = 0;
            var limit = max - min;
            while(Interlocked.CompareExchange(ref pool[index], 1, 0) == 1)
            {
                if(index++ == limit) throw new InvalidOperationException(RanOutOfIdentifiersMessage);
            }

            return (ushort)(min + index);
        }

        public void Return(in ushort identity)
        {
            Interlocked.Exchange(ref pool[identity - min], 0);
        }
    }
}