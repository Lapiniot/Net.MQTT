using System.Buffers;
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

        private readonly byte[] largerBufferSample =
        {
            0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
            0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00
        };

        private readonly byte[] wrongTypesample =
                {
            0x60, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
            0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00
        };

        private readonly byte[] incompleteSample =
        {
            0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f
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
        private ReadOnlySequence<byte> fragmentedSequence;
        private ReadOnlySequence<byte> largerFragmentedSequence;

        public SubscribePacket_TryParse_Should()
        {
            var segment1 = new Segment<byte>(new byte[] { 0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f });

            var segment2 = segment1
                .Append(new byte[] { 0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f })
                .Append(new byte[] { 0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f })
                .Append(new byte[] { 0x68, 0x2f, 0x69, 0x00 });

            var segment3 = segment2.Append(new byte[] { 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69, 0x00 });

            fragmentedSequence = new ReadOnlySequence<byte>(segment1, 0, segment2, 4);
            largerFragmentedSequence = new ReadOnlySequence<byte>(segment1, 0, segment3, 8);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenValidSample()
        {
            var actual = SubscribePacket.TryParse(sample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0].topic);
            Assert.AreEqual(ExactlyOnce, packet.Topics[0].qosLevel);
            Assert.AreEqual("d/e/f", packet.Topics[1].topic);
            Assert.AreEqual(AtLeastOnce, packet.Topics[1].qosLevel);
            Assert.AreEqual("g/h/i", packet.Topics[2].topic);
            Assert.AreEqual(AtMostOnce, packet.Topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenValidFragmentedSample()
        {
            var actual = SubscribePacket.TryParse(fragmentedSequence, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0].topic);
            Assert.AreEqual(ExactlyOnce, packet.Topics[0].qosLevel);
            Assert.AreEqual("d/e/f", packet.Topics[1].topic);
            Assert.AreEqual(AtLeastOnce, packet.Topics[1].qosLevel);
            Assert.AreEqual("g/h/i", packet.Topics[2].topic);
            Assert.AreEqual(AtMostOnce, packet.Topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenLargerBufferSample()
        {
            var actual = SubscribePacket.TryParse(largerBufferSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0].topic);
            Assert.AreEqual(ExactlyOnce, packet.Topics[0].qosLevel);
            Assert.AreEqual("d/e/f", packet.Topics[1].topic);
            Assert.AreEqual(AtLeastOnce, packet.Topics[1].qosLevel);
            Assert.AreEqual("g/h/i", packet.Topics[2].topic);
            Assert.AreEqual(AtMostOnce, packet.Topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenLargerFragmentedBufferSample()
        {
            var actual = SubscribePacket.TryParse(largerFragmentedSequence, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0].topic);
            Assert.AreEqual(ExactlyOnce, packet.Topics[0].qosLevel);
            Assert.AreEqual("d/e/f", packet.Topics[1].topic);
            Assert.AreEqual(AtLeastOnce, packet.Topics[1].qosLevel);
            Assert.AreEqual("g/h/i", packet.Topics[2].topic);
            Assert.AreEqual(AtMostOnce, packet.Topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenIncompleteSample()
        {
            var actual = SubscribePacket.TryParse(incompleteSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenWrongTypeSample()
        {
            var actual = SubscribePacket.TryParse(wrongTypesample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenEmptySample()
        {
            var actual = SubscribePacket.TryParse(new byte[0], out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }
    }
}