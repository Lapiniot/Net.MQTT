using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.SubAckPacketTests
{
    [TestClass]
    public class SubAckPacket_GetBytes_Should
    {
        private readonly SubAckPacket samplePacket = new SubAckPacket(0x02,
            new[] {(byte)QoSLevel.AtLeastOnce, (byte)QoSLevel.AtMostOnce, (byte)QoSLevel.ExactlyOnce});

        [TestMethod]
        public void SetHeaderBytes_0x90_0x05_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            var expectedHeaderFlags = (byte)PacketType.SubAck;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            var expectedRemainingLength = 0x05;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void EncodePacketId_0x0002_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            byte expectedPacketId = 0x0002;
            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodeResultBytes_GivenSampleMessage()
        {
            var bytes = samplePacket.GetBytes().Span;

            Assert.AreEqual(QoSLevel.AtLeastOnce, (QoSLevel)bytes[4]);
            Assert.AreEqual(QoSLevel.AtMostOnce, (QoSLevel)bytes[5]);
            Assert.AreEqual(QoSLevel.ExactlyOnce, (QoSLevel)bytes[6]);
        }
    }
}