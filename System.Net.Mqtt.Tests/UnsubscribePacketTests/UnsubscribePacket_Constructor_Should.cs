using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.UnsubscribePacketTests
{
    [TestClass]
    public class UnsubscribePacket_Constructor_Should
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Throw_ArgumentOutOfRangeException_GivenPacketId0()
        {
            var _ = new UnsubscribePacket(0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Throw_ArgumentNullException_GivenTopicsNull()
        {
            var _ = new UnsubscribePacket(1, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenTopicsEmpty()
        {
            var _ = new UnsubscribePacket(1, Array.Empty<string>());
        }
    }
}