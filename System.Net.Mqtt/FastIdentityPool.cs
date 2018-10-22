using System.Threading;
using static System.Net.Mqtt.Properties.Resources;

namespace System.Net.Mqtt
{
    public class FastIdentityPool : IIdentityPool
    {
        private readonly ushort max;
        private readonly ushort min;
        private readonly int[] pool;

        public FastIdentityPool(ushort minValue = ushort.MinValue, ushort maxValue = ushort.MaxValue)
        {
            if(maxValue < minValue) throw new ArgumentException(string.Format(MustBeGreaterMessageFormat, nameof(maxValue), nameof(minValue)));

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