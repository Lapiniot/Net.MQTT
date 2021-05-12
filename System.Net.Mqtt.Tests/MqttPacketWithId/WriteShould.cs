using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.MqttPacketWithId
{
    [TestClass]
    public class WriteShould
    {
        [TestMethod]
        public void EmitCorrectHeaderRemainingLengthAndPacketIdBigEndian()
        {
            var packet = new UnsubAckPacket(0x11EF);
            var buffer = new byte[4];

            packet.Write(buffer, 2);

            Assert.AreEqual(buffer[0], 0b1011_0000);
            Assert.AreEqual(buffer[1], 2);
            Assert.AreEqual(buffer[2], 0x11);
            Assert.AreEqual(buffer[3], 0xEF);
        }

        [TestMethod]
        public void EmitRemainingLength2AlwaysIgnoringParamValue()
        {
            var packet = new UnsubAckPacket(0x11EF);
            var buffer = new byte[4];

            packet.Write(buffer, 22222);

            Assert.AreEqual(buffer[1], 2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowExceptionGivenUnsufficientBufferSize()
        {
            var packet = new UnsubAckPacket(0x11EF);
            var buffer = new byte[2];

            packet.Write(buffer, 2);
        }
    }
}