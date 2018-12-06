using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.SubAckPacketTests
{
    [TestClass]
    public class SubAckPacket_TryParse_Should
    {
        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenValidSample()
        {
            var sample = new byte[] {0x90, 0x06, 0x00, 0x02, 0x01, 0x00, 0x02, 0x80};

            var actual = SubAckPacket.TryParse(sample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result[0]);
            Assert.AreEqual(0, packet.Result[1]);
            Assert.AreEqual(2, packet.Result[2]);
            Assert.AreEqual(0x80, packet.Result[3]);
        }

        [TestMethod]
        public void ParseOnlyRelevantData_GivenLargerSizeValidSample()
        {
            byte[] largerSizeSample = {0x90, 0x06, 0x00, 0x02, 0x01, 0x00, 0x02, 0x80, 0x00, 0x01, 0x02};
            var actual = SubAckPacket.TryParse(largerSizeSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result[0]);
            Assert.AreEqual(0, packet.Result[1]);
            Assert.AreEqual(2, packet.Result[2]);
            Assert.AreEqual(0x80, packet.Result[3]);
        }

        [TestMethod]
        public void ParseOnlyRelevantData_GivenLargerSizeFragmentedValidSample()
        {
            var segment1 = new Segment<byte>(new byte[] {0x90, 0x06, 0x00});
            var segment2 = segment1.Append(new byte[] {0x02, 0x01, 0x00})
                .Append(new byte[] {0x02, 0x80});
            var segment3 = segment2.Append(new byte[] {0x00, 0x01, 0x02});
            var largerSizeFragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment3, 3);

            var actual = SubAckPacket.TryParse(largerSizeFragmentedSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result[0]);
            Assert.AreEqual(0, packet.Result[1]);
            Assert.AreEqual(2, packet.Result[2]);
            Assert.AreEqual(0x80, packet.Result[3]);
        }

        [TestMethod]
        public void ReturnTrue_PacketNotNull_GivenValidFragmentedSample()
        {
            var segment1 = new Segment<byte>(new byte[] {0x90});
            var segment2 = segment1.Append(new byte[] {0x06}).Append(new byte[] {0x00})
                .Append(new byte[] {0x02}).Append(new byte[] {0x01, 0x00})
                .Append(new byte[] {0x02, 0x80});
            var fragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 2);

            var actual = SubAckPacket.TryParse(fragmentedSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Result.Length);
            Assert.AreEqual(1, packet.Result[0]);
            Assert.AreEqual(0, packet.Result[1]);
            Assert.AreEqual(2, packet.Result[2]);
            Assert.AreEqual(0x80, packet.Result[3]);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenIncompleteSample()
        {
            var actual = SubAckPacket.TryParse(new byte[] {0x90, 0x06, 0x00, 0x02, 0x01}, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenWrongTypeSample()
        {
            byte[] wrongTypeSample = {0x12, 0x06, 0x00, 0x02, 0x01, 0x00, 0x02, 0x80};

            var actual = SubAckPacket.TryParse(wrongTypeSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_PacketNull_GivenEmptySample()
        {
            var actual = SubAckPacket.TryParse(new byte[0], out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }
    }
}