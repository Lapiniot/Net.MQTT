using System.Buffers;
using System.Linq;
using System.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubscribePacket
{
    [TestClass]
    public class TryParseShould
    {
        private readonly ReadOnlySequence<byte> fragmentedSequence;

        private readonly byte[] incompleteSample =
        {
            0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f
        };

        private readonly byte[] largerBufferSample =
        {
            0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
            0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00
        };

        private readonly ReadOnlySequence<byte> largerFragmentedSequence;

        private readonly byte[] sample =
        {
            0x82, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
            0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00
        };

        private readonly byte[] wrongTypeSample =
        {
            0x60, 0x1a, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x02, 0x00, 0x05, 0x64, 0x2f,
            0x65, 0x2f, 0x66, 0x01, 0x00, 0x05, 0x67, 0x2f,
            0x68, 0x2f, 0x69, 0x00
        };

        public TryParseShould()
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
        public void ReturnTruePacketNotNullGivenValidSample()
        {
            var actual = Packets.SubscribePacket.TryRead(sample, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(sample.Length, consumed);
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0].topic);
            Assert.AreEqual(2, topics[0].qosLevel);
            Assert.AreEqual("d/e/f", topics[1].topic);
            Assert.AreEqual(1, topics[1].qosLevel);
            Assert.AreEqual("g/h/i", topics[2].topic);
            Assert.AreEqual(0, topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullGivenValidFragmentedSample()
        {
            var actual = Packets.SubscribePacket.TryRead(fragmentedSequence, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(sample.Length, consumed);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0].topic);
            Assert.AreEqual(2, topics[0].qosLevel);
            Assert.AreEqual("d/e/f", topics[1].topic);
            Assert.AreEqual(1, topics[1].qosLevel);
            Assert.AreEqual("g/h/i", topics[2].topic);
            Assert.AreEqual(0, topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullConsumed28GivenLargerBufferSample()
        {
            var actual = Packets.SubscribePacket.TryRead(largerBufferSample, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(28, consumed);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0].topic);
            Assert.AreEqual(2, topics[0].qosLevel);
            Assert.AreEqual("d/e/f", topics[1].topic);
            Assert.AreEqual(1, topics[1].qosLevel);
            Assert.AreEqual("g/h/i", topics[2].topic);
            Assert.AreEqual(0, topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullConsumed28GivenLargerFragmentedBufferSample()
        {
            var actual = Packets.SubscribePacket.TryRead(largerFragmentedSequence, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(28, consumed);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0].topic);
            Assert.AreEqual(2, topics[0].qosLevel);
            Assert.AreEqual("d/e/f", topics[1].topic);
            Assert.AreEqual(1, topics[1].qosLevel);
            Assert.AreEqual("g/h/i", topics[2].topic);
            Assert.AreEqual(0, topics[2].qosLevel);
        }

        [TestMethod]
        public void ReturnFalsePacketNullConsumed0GivenIncompleteSample()
        {
            var actual = Packets.SubscribePacket.TryRead(incompleteSample, out var packet, out var consumed);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
            Assert.AreEqual(0, consumed);
        }

        [TestMethod]
        public void ReturnFalsePacketNullConsumed0GivenWrongTypeSample()
        {
            var actual = Packets.SubscribePacket.TryRead(wrongTypeSample, out var packet, out var consumed);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
            Assert.AreEqual(0, consumed);
        }

        [TestMethod]
        public void ReturnFalsePacketNullGivenEmptySample()
        {
            var actual = Packets.SubscribePacket.TryRead(Array.Empty<byte>(), out var packet, out var consumed);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
            Assert.AreEqual(0, consumed);
        }
    }
}