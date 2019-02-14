using System.Buffers;
using System.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Extensions
{
    [TestClass]
    public class SequenceExtensions_TryReadMqttHeader_Should
    {
        [TestMethod]
        public void ReturnFalse_GivenEmptySample()
        {
            var actual = SpanExtensions.TryReadMqttHeader(Span<byte>.Empty, out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptySequence()
        {
            var actual = new ReadOnlySequence<byte>().TryReadMqttHeader(out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenIncompleteSample()
        {
            var incompleteSample = new byte[] {64, 205, 255, 255};

            var actual = SpanExtensions.TryReadMqttHeader(incompleteSample, out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenIncompleteSequence()
        {
            var segment = new Segment<byte>(new byte[] {64, 205});
            var incompleteSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] {255, 255}), 2);

            var actual = incompleteSequence.TryReadMqttHeader(out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenWrongSample()
        {
            var wrongSample = new byte[] {64, 205, 255, 255, 255, 127, 0};

            var actual = SpanExtensions.TryReadMqttHeader(wrongSample, out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenWrongSequence()
        {
            var segment = new Segment<byte>(new byte[] {64, 205});
            var wrongSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] {255, 255}).Append(new byte[] {255, 127, 0}), 3);

            var actual = wrongSequence.TryReadMqttHeader(out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenCompleteSample()
        {
            var completeSample = new byte[] {64, 205, 255, 255, 127, 0, 0};

            var actual = SpanExtensions.TryReadMqttHeader(completeSample, out _, out _, out _);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenCompleteSequence()
        {
            var segment = new Segment<byte>(new byte[] {64, 205});
            var completeSequence = new ReadOnlySequence<byte>(segment, 0,
                segment.Append(new byte[] {255, 255}).Append(new byte[] {127, 0, 0}), 3);

            var actual = completeSequence.TryReadMqttHeader(out _, out _, out _);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnPacketFlags64_GivenCompleteSample()
        {
            const int expectedFlags = 64;

            var completeSample = new byte[] {64, 205, 255, 255, 127, 0, 0};

            SpanExtensions.TryReadMqttHeader(completeSample, out var actualFlags, out _, out _);

            Assert.AreEqual(expectedFlags, actualFlags);
        }

        [TestMethod]
        public void ReturnPacketFlags64_GivenCompleteSequence()
        {
            const int expectedFlags = 64;

            var segment = new Segment<byte>(new byte[] {64, 205});
            var completeSequence = new ReadOnlySequence<byte>(segment, 0,
                segment.Append(new byte[] {255, 255}).Append(new byte[] {127, 0, 0}), 3);

            completeSequence.TryReadMqttHeader(out var actualFlags, out _, out _);

            Assert.AreEqual(expectedFlags, actualFlags);
        }

        [TestMethod]
        public void ReturnLength268435405_GivenCompleteSample()
        {
            const int expectedLength = 268435405;

            var completeSample = new byte[] {64, 205, 255, 255, 127, 0, 0};

            SpanExtensions.TryReadMqttHeader(completeSample, out _, out var actualLength, out _);

            Assert.AreEqual(expectedLength, actualLength);
        }

        [TestMethod]
        public void ReturnLength268435405_GivenCompleteSequence()
        {
            const int expectedLength = 268435405;

            var segment = new Segment<byte>(new byte[] {64, 205});
            var completeSequence = new ReadOnlySequence<byte>(segment, 0,
                segment.Append(new byte[] {255, 255}).Append(new byte[] {127, 0, 0}), 3);

            completeSequence.TryReadMqttHeader(out _, out var actualLength, out _);

            Assert.AreEqual(expectedLength, actualLength);
        }

        [TestMethod]
        public void ReturnDataOffset5_GivenCompleteSample()
        {
            const int expectedDataOffset = 5;

            var completeSequence = new byte[] {64, 205, 255, 255, 127, 0, 0};

            SpanExtensions.TryReadMqttHeader(completeSequence, out _, out _, out var actualDataOffset);

            Assert.AreEqual(expectedDataOffset, actualDataOffset);
        }

        [TestMethod]
        public void ReturnDataOffset5_GivenCompleteSequence()
        {
            const int expectedDataOffset = 5;

            var segment = new Segment<byte>(new byte[] {64, 205});
            var completeSequence = new ReadOnlySequence<byte>(segment, 0,
                segment.Append(new byte[] {255, 255}).Append(new byte[] {127, 0, 0}), 3);

            completeSequence.TryReadMqttHeader(out _, out _, out var actualDataOffset);

            Assert.AreEqual(expectedDataOffset, actualDataOffset);
        }
    }
}