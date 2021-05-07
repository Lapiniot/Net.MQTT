using System.Buffers;
using System.Linq;
using System.Memory;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.UnsubscribePacketTests
{
    [TestClass]
    public class UnsubscribePacketTryParseShould
    {
        private readonly ReadOnlySequence<byte> fragmentedSequence;

        private readonly byte[] incompleteSample =
        {
            0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65
        };

        private readonly byte[] largerBufferSample =
        {
            0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
            0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
            0x69, 0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68,
            0x2f
        };

        private readonly ReadOnlySequence<byte> largerFragmentedSequence;

        private readonly byte[] sample =
        {
            0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
            0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
            0x69
        };

        private readonly byte[] wrongTypeSample =
        {
            0x12, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f,
            0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65,
            0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f,
            0x69
        };

        public UnsubscribePacketTryParseShould()
        {
            var segment1 = new Segment<byte>(new byte[] {0xa2, 0x17, 0x00, 0x02, 0x00, 0x05, 0x61, 0x2f});

            var segment2 = segment1
                .Append(new byte[] {0x62, 0x2f, 0x63, 0x00, 0x05, 0x64, 0x2f, 0x65})
                .Append(new byte[] {0x2f, 0x66, 0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69});

            fragmentedSequence = new ReadOnlySequence<byte>(segment1, 0, segment2, 9);

            var segment3 = segment2.Append(new byte[] {0x00, 0x05, 0x67, 0x2f, 0x68, 0x2f, 0x69});
            largerFragmentedSequence = new ReadOnlySequence<byte>(segment1, 0, segment3, 7);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullConsumed25GivenValidSample()
        {
            var actual = UnsubscribePacket.TryRead(sample, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(25, consumed);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0]);
            Assert.AreEqual("d/e/f", topics[1]);
            Assert.AreEqual("g/h/i", topics[2]);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullConsumed25GivenValidFragmentedSample()
        {
            var actual = UnsubscribePacket.TryRead(fragmentedSequence, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(25, consumed);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0]);
            Assert.AreEqual("d/e/f", topics[1]);
            Assert.AreEqual("g/h/i", topics[2]);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullConsumed25GivenLargerBufferSample()
        {
            var actual = UnsubscribePacket.TryRead(largerBufferSample, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(25, consumed);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0]);
            Assert.AreEqual("d/e/f", topics[1]);
            Assert.AreEqual("g/h/i", topics[2]);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullConsumed25GivenLargerFragmentedBufferSample()
        {
            var actual = UnsubscribePacket.TryRead(largerFragmentedSequence, out var packet, out var consumed);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(25, consumed);
            var topics = packet.Topics.ToArray();
            Assert.AreEqual(3, topics.Length);
            Assert.AreEqual("a/b/c", topics[0]);
            Assert.AreEqual("d/e/f", topics[1]);
            Assert.AreEqual("g/h/i", topics[2]);
        }

        [TestMethod]
        public void ReturnFalsePacketNullConsumed0GivenIncompleteSample()
        {
            var actual = UnsubscribePacket.TryRead(incompleteSample, out var packet, out var consumed);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
            Assert.AreEqual(0, consumed);
        }

        [TestMethod]
        public void ReturnFalsePacketNullConsumed0GivenWrongTypeSample()
        {
            var actual = UnsubscribePacket.TryRead(wrongTypeSample, out var packet, out var consumed);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
            Assert.AreEqual(0, consumed);
        }

        [TestMethod]
        public void ReturnFalsePacketNullConsumed0GivenEmptySample()
        {
            var actual = UnsubscribePacket.TryRead(Array.Empty<byte>(), out var packet, out var consumed);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
            Assert.AreEqual(0, consumed);
        }
    }
}