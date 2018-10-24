using System.Buffers;
using System.Net.Mqtt.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.PacketType;

namespace System.Net.Mqtt.MqttPacketWithIdTests
{
    [TestClass]
    public class MqttPacketWithId_TryParseGeneric_Should
    {
        [TestMethod]
        public void ReturnTrue_AndPacketId_GivenValidSample()
        {
            var validSample = new byte[] {(byte)PubAck, 0x02, 0x11, 0x22};

            var actual = MqttPacketWithId.TryParseGeneric(validSample, PubAck, out var id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);
        }

        [TestMethod]
        public void ReturnTrue_AndPacketId_GivenValidSequence()
        {
            var contiguousSequence = new ReadOnlySequence<byte>(new byte[] {(byte)PubAck, 0x02, 0x11, 0x22});

            var actual = MqttPacketWithId.TryParseGeneric(contiguousSequence, PubAck, out var id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            var start = new Segment<byte>(new byte[] {(byte)PubAck, 0x02, 0x11});
            var fragmentedSequence1 = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x22}), 1);

            actual = MqttPacketWithId.TryParseGeneric(fragmentedSequence1, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            start = new Segment<byte>(new byte[] {(byte)PubAck, 0x02});
            var fragmentedSequence2 = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x11, 0x22}), 2);

            actual = MqttPacketWithId.TryParseGeneric(fragmentedSequence2, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            start = new Segment<byte>(new[] {(byte)PubAck});
            var fragmentedSequence3 = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x02, 0x11, 0x22}), 3);

            actual = MqttPacketWithId.TryParseGeneric(fragmentedSequence3, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);

            start = new Segment<byte>(new[] {(byte)PubAck});

            var fragmentedSequence4 = new ReadOnlySequence<byte>(start, 0, start
                .Append(new byte[] {0x02})
                .Append(new byte[] {0x11})
                .Append(new byte[] {0x22}), 1);

            actual = MqttPacketWithId.TryParseGeneric(fragmentedSequence4, PubAck, out id);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x1122, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenEmptySample()
        {
            var emptySample = ReadOnlySpan<byte>.Empty;

            var actual = MqttPacketWithId.TryParseGeneric(emptySample, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenEmptySequence()
        {
            var emptySequence = new ReadOnlySequence<byte>();

            var actual = MqttPacketWithId.TryParseGeneric(emptySequence, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenWrongTypeSample()
        {
            var wrongTypeSample = new byte[] {(byte)PubRel, 0x02, 0x11, 0x22};

            var actual = MqttPacketWithId.TryParseGeneric(wrongTypeSample, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenWrongTypeSequence()
        {
            var start = new Segment<byte>(new[] {(byte)PubRec});

            var wrongTypeSequence = new ReadOnlySequence<byte>(start, 0, start
                .Append(new byte[] {0x02})
                .Append(new byte[] {0x11})
                .Append(new byte[] {0x22}), 1);

            var actual = MqttPacketWithId.TryParseGeneric(wrongTypeSequence, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenWrongSizeSample()
        {
            var wrongSizeSample = new byte[] {(byte)PubRel, 0x00, 0x11, 0x22};

            var actual = MqttPacketWithId.TryParseGeneric(wrongSizeSample, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenWrongSizeSequence()
        {
            var start = new Segment<byte>(new[] {(byte)PubAck});
            var wrongSizeSequence = new ReadOnlySequence<byte>(start, 0, start
                .Append(new byte[] {0x00})
                .Append(new byte[] {0x11})
                .Append(new byte[] {0x22}), 1);

            var actual = MqttPacketWithId.TryParseGeneric(wrongSizeSequence, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenIncompleteSample()
        {
            var incompleteSample = new byte[] {(byte)PubAck, 0x02, 0x11};

            var actual = MqttPacketWithId.TryParseGeneric(incompleteSample, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketId_0_GivenIncompleteSequence()
        {
            var start = new Segment<byte>(new[] {(byte)PubAck});
            var incompleteSequence = new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {0x02}), 1);

            var actual = MqttPacketWithId.TryParseGeneric(incompleteSequence, PubAck, out var id);

            Assert.IsFalse(actual);
            Assert.AreEqual(0x00, id);
        }
    }
}