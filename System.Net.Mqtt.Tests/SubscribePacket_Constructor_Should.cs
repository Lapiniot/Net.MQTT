using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
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
    }
}