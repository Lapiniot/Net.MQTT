using System.Buffers;
using System.Net.Mqtt.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.MqttPacketWithIdTests
{
    [TestClass]
    public class MqttPacketWithId_TryParseGeneric_Should
    {
        private readonly byte[] incompleteSample = {(byte)PubAck, 0x02, 0x11};
        private readonly byte[] sample = {(byte)PubAck, 0x02, 0x11, 0x22};
        private readonly ReadOnlySequence<byte> sequence1;
        private readonly ReadOnlySequence<byte> sequence2;
        private readonly ReadOnlySequence<byte> sequence3;
        private readonly ReadOnlySequence<byte> sequence4;
        private readonly ReadOnlySequence<byte> sequence5;
        private readonly byte[] wrongSizeSample = {(byte)PubRel, 0x20, 0x11, 0x22};
        private readonly byte[] wrongTypeSample = {(byte)PubRel, 0x02, 0x11, 0x22};

        public MqttPacketWithId_TryParseGeneric_Should()
        {
            sequence1 = new ReadOnlySequence<byte>(sample);

            var start = new Segment<byte>(new byte[] {(byte)PubAck, 0x02, 0x11});

            sequence2 = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x22}), 1);

            start = new Segment<byte>(new byte[] {(byte)PubAck, 0x02});

            sequence3 = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x11, 0x22}), 2);

            start = new Segment<byte>(new[] {(byte)PubAck});

            sequence4 = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x02, 0x11, 0x22}), 3);

            start = new Segment<byte>(new[] {(byte)PubAck});

            sequence5 = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x02})
                .Append(new byte[] {0x11}).Append(new byte[] {0x22}), 1);
        }

        [TestMethod]
        public void ReturnTrue_AndPacketId_GivenValidSample()
        {
            var actual = MqttPacketWithId.TryParseGeneric(sample, PubAck, out var id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);
        }

        [TestMethod]
        public void ReturnTrue_AndPacketId_GivenValidSequence()
        {
            var actual = MqttPacketWithId.TryParseGeneric(sequence1, PubAck, out var id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            actual = MqttPacketWithId.TryParseGeneric(sequence2, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            actual = MqttPacketWithId.TryParseGeneric(sequence3, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            actual = MqttPacketWithId.TryParseGeneric(sequence4, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            actual = MqttPacketWithId.TryParseGeneric(sequence5, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenWrongTypeSample()
        {
            var actual = MqttPacketWithId.TryParseGeneric(wrongTypeSample, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenWrongSizeSample()
        {
            var actual = MqttPacketWithId.TryParseGeneric(wrongSizeSample, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenIncompleteSample()
        {
            var actual = MqttPacketWithId.TryParseGeneric(incompleteSample, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }
    }
}