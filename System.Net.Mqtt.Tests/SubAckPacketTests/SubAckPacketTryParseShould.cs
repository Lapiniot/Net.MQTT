using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubAckPacketTests
{
    [TestClass]
    public class SubAckPacketTryParseShould
    {
        [TestMethod]
        public void ReturnTruePacketNotNullGivenValidSample()
        {
            var sample = new byte[] {0x90, 0x06, 0x00, 0x02, 0x01, 0x00, 0x02, 0x80};

            var actual = SubAckPacket.TryRead(sample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result.Span[0]);
            Assert.AreEqual(0, packet.Result.Span[1]);
            Assert.AreEqual(2, packet.Result.Span[2]);
            Assert.AreEqual(0x80, packet.Result.Span[3]);
        }

        [TestMethod]
        public void ParseOnlyRelevantDataGivenLargerSizeValidSample()
        {
            byte[] largerSizeSample = {0x90, 0x06, 0x00, 0x02, 0x01, 0x00, 0x02, 0x80, 0x00, 0x01, 0x02};
            var actual = SubAckPacket.TryRead(largerSizeSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result.Span[0]);
            Assert.AreEqual(0, packet.Result.Span[1]);
            Assert.AreEqual(2, packet.Result.Span[2]);
            Assert.AreEqual(0x80, packet.Result.Span[3]);
        }

        [TestMethod]
        public void ParseOnlyRelevantDataGivenLargerSizeFragmentedValidSample()
        {
            var segment1 = new Segment<byte>(new byte[] {0x90, 0x06, 0x00});
            var segment2 = segment1.Append(new byte[] {0x02, 0x01, 0x00})
                .Append(new byte[] {0x02, 0x80});
            var segment3 = segment2.Append(new byte[] {0x00, 0x01, 0x02});
            var largerSizeFragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment3, 3);

            var actual = SubAckPacket.TryRead(largerSizeFragmentedSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result.Span[0]);
            Assert.AreEqual(0, packet.Result.Span[1]);
            Assert.AreEqual(2, packet.Result.Span[2]);
            Assert.AreEqual(0x80, packet.Result.Span[3]);
        }

        [TestMethod]
        public void ReturnTruePacketNotNullGivenValidFragmentedSample()
        {
            var segment1 = new Segment<byte>(new byte[] {0x90});
            var segment2 = segment1.Append(new byte[] {0x06}).Append(new byte[] {0x00})
                .Append(new byte[] {0x02}).Append(new byte[] {0x01, 0x00})
                .Append(new byte[] {0x02, 0x80});
            var fragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 2);

            var actual = SubAckPacket.TryRead(fragmentedSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result.Span[0]);
            Assert.AreEqual(0, packet.Result.Span[1]);
            Assert.AreEqual(2, packet.Result.Span[2]);
            Assert.AreEqual(0x80, packet.Result.Span[3]);
        }

        [TestMethod]
        public void ReturnFalsePacketNullGivenIncompleteSample()
        {
            var actual = SubAckPacket.TryRead(new byte[] {0x90, 0x06, 0x00, 0x02, 0x01}, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalsePacketNullGivenWrongTypeSample()
        {
            byte[] wrongTypeSample = {0x12, 0x06, 0x00, 0x02, 0x01, 0x00, 0x02, 0x80};

            var actual = SubAckPacket.TryRead(wrongTypeSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalsePacketNullGivenEmptySample()
        {
            var actual = SubAckPacket.TryRead(Array.Empty<byte>(), out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }
    }
}