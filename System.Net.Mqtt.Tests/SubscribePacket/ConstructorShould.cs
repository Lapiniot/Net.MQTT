using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubscribePacket
{
    [TestClass]
    public class ConstructorShould
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowArgumentOutOfRangeExceptionGivenPacketId0()
        {
            var _ = new Packets.SubscribePacket(0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowArgumentNullExceptionGivenTopicsNull()
        {
            var _ = new Packets.SubscribePacket(1, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowArgumentExceptionGivenTopicsEmpty()
        {
            var _ = new Packets.SubscribePacket(1, Array.Empty<(string, byte)>());
        }
    }
}