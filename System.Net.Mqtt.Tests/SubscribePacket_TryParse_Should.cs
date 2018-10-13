using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.QoSLevel;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class SubscribePacket_TryParse_Should
    {
        private readonly byte[] sample =
        {
            0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
            0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00
        };

        private readonly SubscribePacket samplePacket = new SubscribePacket(2)
        {
            Topics =
            {
                ("a/b/c", ExactlyOnce),
                ("d/e/f", AtLeastOnce),
                ("g/h/i", AtMostOnce)
            }
        };

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenValidSample()
        {
            var actual = SubscribePacket.TryParse(sample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0]);
            Assert.AreEqual("d/e/f", packet.Topics[1]);
            Assert.AreEqual("g/h/i", packet.Topics[2]);
        }
    }
}