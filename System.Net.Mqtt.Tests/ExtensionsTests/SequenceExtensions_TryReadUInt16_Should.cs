using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ExtensionsTests
{
    [TestClass]
    public class SequenceExtensions_TryReadUInt16_Should
    {
        private readonly ReadOnlySequence<byte> completeSequence;
        private readonly ReadOnlySequence<byte> emptySequence;
        private readonly ReadOnlySequence<byte> fragmentedSequence;
        private readonly ReadOnlySequence<byte> incompleteSequence;

        public SequenceExtensions_TryReadUInt16_Should()
        {
            completeSequence = new ReadOnlySequence<byte>(new byte[] {0x40, 0xCD});
            emptySequence = new ReadOnlySequence<byte>(new byte[0]);
            incompleteSequence = new ReadOnlySequence<byte>(new byte[] {0x40});
            var segment = new Segment<byte>(new byte[] {0x40});
            fragmentedSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] {0xFF}), 1);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptySequence()
        {
            var actual = emptySequence.TryReadUInt16(out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenIncompleteSequence()
        {
            var actual = incompleteSequence.TryReadUInt16(out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenCompleteSequence()
        {
            const int expectedValue = 0x40cd;

            var actual = completeSequence.TryReadUInt16(out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void ReturnTrue_GivenFragmentedSequence()
        {
            const int expectedValue = 0x40FF;

            var actual = fragmentedSequence.TryReadUInt16(out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}