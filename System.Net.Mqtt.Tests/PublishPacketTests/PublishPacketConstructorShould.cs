using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.PublishPacketTests
{
    [TestClass]
    public class PublishPacketConstructorShould
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowArgumentExceptionGivenTopicNull()
        {
            var _ = new PublishPacket(0, default, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowArgumentExceptionGivenTopicEmpty()
        {
            var _ = new PublishPacket(0, default, string.Empty);
        }

        [TestMethod]
        public void NotThrowArgumentExceptionGivenQoS0AndNoPacketId()
        {
            var _ = new PublishPacket(0, 0, "/");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowArgumentExceptionGivenQoS1AndNoPacketId()
        {
            var _ = new PublishPacket(0, 1, "/");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowArgumentExceptionGivenQoS2AndNoPacketId()
        {
            var _ = new PublishPacket(0, 2, "/");
        }
    }
}