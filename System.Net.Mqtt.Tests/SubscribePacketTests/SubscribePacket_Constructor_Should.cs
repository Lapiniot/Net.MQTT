using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.SubscribePacketTests
{
    [TestClass]
    public class SubscribePacket_Constructor_Should
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Throw_ArgumentOutOfRangeException_GivenPacketId0()
        {
            var _ = new SubscribePacket(0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Throw_ArgumentNullException_GivenTopicsNull()
        {
            var _ = new SubscribePacket(1, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenTopicsEmpty()
        {
            var _ = new SubscribePacket(1, Array.Empty<(string, QoSLevel)>());
        }
    }
}