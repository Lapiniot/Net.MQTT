using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.UnsubscribePacketTests
{
    [TestClass]
    public class UnsubscribePacketConstructorShould
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowArgumentOutOfRangeExceptionGivenPacketId0()
        {
            var _ = new UnsubscribePacket(0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowArgumentNullExceptionGivenTopicsNull()
        {
            var _ = new UnsubscribePacket(1, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowArgumentExceptionGivenTopicsEmpty()
        {
            var _ = new UnsubscribePacket(1, Array.Empty<string>());
        }
    }
}