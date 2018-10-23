using System.Buffers;
using System.Net.Mqtt.Buffers;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.UnsubscribePacketTests
{
    [TestClass]
    public class UnsubscribePacket_TryParse_Should
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

        public UnsubscribePacket_TryParse_Should()
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
        public void ReturnTrue_PacketNotNull_GivenValidSample()
        {
            var actual = UnsubscribePacket.TryParse(sample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0]);
            Assert.AreEqual("d/e/f", packet.Topics[1]);
            Assert.AreEqual("g/h/i", packet.Topics[2]);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenValidFragmentedSample()
        {
            var actual = UnsubscribePacket.TryParse(fragmentedSequence, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0]);
            Assert.AreEqual("d/e/f", packet.Topics[1]);
            Assert.AreEqual("g/h/i", packet.Topics[2]);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenLargerBufferSample()
        {
            var actual = UnsubscribePacket.TryParse(largerBufferSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0]);
            Assert.AreEqual("d/e/f", packet.Topics[1]);
            Assert.AreEqual("g/h/i", packet.Topics[2]);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenLargerFragmentedBufferSample()
        {
            var actual = UnsubscribePacket.TryParse(largerFragmentedSequence, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Topics.Count);
            Assert.AreEqual("a/b/c", packet.Topics[0]);
            Assert.AreEqual("d/e/f", packet.Topics[1]);
            Assert.AreEqual("g/h/i", packet.Topics[2]);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenIncompleteSample()
        {
            var actual = UnsubscribePacket.TryParse(incompleteSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenWrongTypeSample()
        {
            var actual = UnsubscribePacket.TryParse(wrongTypeSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenEmptySample()
        {
            var actual = UnsubscribePacket.TryParse(new byte[0], out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }
    }
}