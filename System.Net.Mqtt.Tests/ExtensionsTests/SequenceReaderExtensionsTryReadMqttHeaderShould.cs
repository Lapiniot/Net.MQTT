using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ExtensionsTests
{
    [TestClass]
    public class SequenceReaderExtensionsTryReadMqttHeaderShould
    {
        [TestMethod]
        public void ReturnFalseGivenEmptySample()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>());

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, header);
            Assert.AreEqual(0, length);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseGivenIncompleteSampleOneByte()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] {64}));

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, header);
            Assert.AreEqual(0, length);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseGivenIncompleteSample()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] {64, 205, 255, 255}));

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, header);
            Assert.AreEqual(0, length);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseGivenIncompleteSequence()
        {
            var segment = new Segment<byte>(new byte[] {64, 205});

            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] {255, 255}), 2));

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, header);
            Assert.AreEqual(0, length);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseGivenWrongSample()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] {64, 205, 255, 255, 255, 127, 0}));

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, header);
            Assert.AreEqual(0, length);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseGivenWrongSequence()
        {
            var segment = new Segment<byte>(new byte[] {64, 205});

            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(segment, 0,
                segment.Append(new byte[] {255, 255}).Append(new byte[] {255, 127, 0}), 3));

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, header);
            Assert.AreEqual(0, length);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueGivenCompleteSample()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] {64, 205, 255, 255, 127, 0, 0}));

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x40, header);
            Assert.AreEqual(0x0fffffcd, length);
            Assert.AreEqual(5, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueGivenCompleteSequence()
        {
            var start = new Segment<byte>(new byte[] {64, 205});

            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(start, 0, start.Append(new byte[] {255, 255}).Append(new byte[] {127, 0, 0}), 3));

            var actual = reader.TryReadMqttHeader(out var header, out var length);

            Assert.IsTrue(actual);
            Assert.AreEqual(0x40, header);
            Assert.AreEqual(0x0fffffcd, length);
            Assert.AreEqual(5, reader.Consumed);
        }
    }
}