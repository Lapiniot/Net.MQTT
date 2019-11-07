using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubAckPacketTests
{
    [TestClass]
    public class SubAckPacket_Constructor_Should
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Throw_ArgumentOutOfRangeException_GivenPacketId0()
        {
            var _ = new SubAckPacket(0, new byte[] {0});
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Throw_ArgumentNullException_GivenResultParamNull()
        {
            var _ = new SubAckPacket(1, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenResultParamEmpty()
        {
            var _ = new SubAckPacket(1, Array.Empty<byte>());
        }
    }
}