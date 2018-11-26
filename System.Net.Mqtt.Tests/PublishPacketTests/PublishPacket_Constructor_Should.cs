using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.PublishPacketTests
{
    [TestClass]
    public class PublishPacket_Constructor_Should
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenTopicNull()
        {
            var _ = new PublishPacket(0, default, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenTopicEmpty()
        {
            var _ = new PublishPacket(0, default, string.Empty);
        }

        [TestMethod]
        public void NotThrow_ArgumentException_GivenQoS0AndNoPacketId()
        {
            var _ = new PublishPacket(0, 0, "/");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenQoS1AndNoPacketId()
        {
            var _ = new PublishPacket(0, 1, "/");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenQoS2AndNoPacketId()
        {
            var _ = new PublishPacket(0, 2, "/");
        }
    }
}