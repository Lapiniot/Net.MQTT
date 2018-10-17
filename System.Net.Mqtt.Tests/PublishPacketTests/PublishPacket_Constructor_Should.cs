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
            var _ = new PublishPacket(null, Memory<byte>.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Throw_ArgumentException_GivenTopicEmpty()
        {
            var _ = new PublishPacket(string.Empty, Memory<byte>.Empty);
        }
    }
}