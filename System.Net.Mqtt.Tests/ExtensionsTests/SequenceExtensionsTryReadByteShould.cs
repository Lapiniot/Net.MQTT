using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ExtensionsTests
{
    [TestClass]
    public class SequenceExtensionsTryReadByteShould
    {
        private readonly ReadOnlySequence<byte> completeSequence;
        private readonly ReadOnlySequence<byte> emptySequence;
        private readonly ReadOnlySequence<byte> fragmentedSequence;

        public SequenceExtensionsTryReadByteShould()
        {
            completeSequence = new ReadOnlySequence<byte>(new byte[] {0x40});
            emptySequence = new ReadOnlySequence<byte>(Array.Empty<byte>());
            var segment = new Segment<byte>(Array.Empty<byte>());
            fragmentedSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] {0x40}), 1);
        }

        [TestMethod]
        public void ReturnFalseGivenEmptySequence()
        {
            var actual = emptySequence.TryReadByte(out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenCompleteSequence()
        {
            const int expectedValue = 0x40;

            var actual = completeSequence.TryReadByte(out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void ReturnTrueGivenFragmentedSequence()
        {
            const int expectedValue = 0x40;

            var actual = fragmentedSequence.TryReadByte(out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}