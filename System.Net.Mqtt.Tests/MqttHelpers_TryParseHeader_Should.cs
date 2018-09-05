using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class MqttHelpers_TryParseHeader_Should
    {
        private readonly ReadOnlySequence<byte> completeSequence;
        private readonly ReadOnlySequence<byte> emptySequence;
        private readonly ReadOnlySequence<byte> fragmentedSequence;
        private readonly ReadOnlySequence<byte> incompleteSequence;
        private readonly ReadOnlySequence<byte> wrongSequence;

        public MqttHelpers_TryParseHeader_Should()
        {
            completeSequence = new ReadOnlySequence<byte>(new byte[] {64, 205, 255, 255, 127, 0, 0});
            emptySequence = new ReadOnlySequence<byte>(new byte[0]);
            incompleteSequence = new ReadOnlySequence<byte>(new byte[] {64, 205, 255, 255});
            wrongSequence = new ReadOnlySequence<byte>(new byte[] {64, 205, 255, 255, 255, 127, 0});

            var segment = new Segment<byte>(new byte[] {64, 205});
            fragmentedSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] {255, 255, 127, 0, 0}), 5);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptySequence()
        {
            var actual = MqttHelpers.TryParseHeader(emptySequence, out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenIncompleteSequence()
        {
            var actual = MqttHelpers.TryParseHeader(incompleteSequence, out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenWrongSequence()
        {
            var actual = MqttHelpers.TryParseHeader(wrongSequence, out _, out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenCompleteSequence()
        {
            var actual = MqttHelpers.TryParseHeader(completeSequence, out _, out _, out _);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnPacketFlags64_GivenCompleteSequence()
        {
            var expectedFlags = 64;

            MqttHelpers.TryParseHeader(completeSequence, out var actualFlags, out _, out _);

            Assert.AreEqual(expectedFlags, actualFlags);
        }

        [TestMethod]
        public void ReturnLength268435405_GivenCompleteSequence()
        {
            var expectedLength = 268435405;

            MqttHelpers.TryParseHeader(completeSequence, out _, out var actualLength, out _);

            Assert.AreEqual(expectedLength, actualLength);
        }

        [TestMethod]
        public void ReturnDataOffset5_GivenCompleteSequence()
        {
            var expectedDataOffset = 5;

            MqttHelpers.TryParseHeader(completeSequence, out _, out _, out var actualDataOffset);

            Assert.AreEqual(expectedDataOffset, actualDataOffset);
        }

        [TestMethod]
        public void ReturnDataOffset5_GivenFragmentedSequence()
        {
            var expectedDataOffset = 5;

            MqttHelpers.TryParseHeader(fragmentedSequence, out _, out _, out var actualDataOffset);

            Assert.AreEqual(expectedDataOffset, actualDataOffset);
        }

        [TestMethod]
        public void ReturnTrue_GivenFragmentedSequence()
        {
            var actual = MqttHelpers.TryParseHeader(fragmentedSequence, out _, out _, out _);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnPacketFlags64_GivenFragmentedSequence()
        {
            var expectedFlags = 64;

            MqttHelpers.TryParseHeader(fragmentedSequence, out var actualFlags, out _, out _);

            Assert.AreEqual(expectedFlags, actualFlags);
        }

        [TestMethod]
        public void ReturnLength268435405_GivenFragmentedSequence()
        {
            var expectedLength = 268435405;

            MqttHelpers.TryParseHeader(fragmentedSequence, out _, out var actualLength, out _);

            Assert.AreEqual(expectedLength, actualLength);
        }
    }

    internal class Segment<T> : ReadOnlySequenceSegment<T>
    {
        public Segment(T[] array)
        {
            Memory = array;
        }

        public ReadOnlySequenceSegment<T> Append(T[] array)
        {
            return Next = new Segment<T>(array) {RunningIndex = RunningIndex + Memory.Length};
        }
    }
}