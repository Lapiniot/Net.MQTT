using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ExtensionsTests
{
    [TestClass]
    public class SequenceExtensionsTryReadMqttStringShould
    {
        private readonly ReadOnlySequence<byte> completeSequence;
        private readonly ReadOnlySequence<byte> emptySequence;
        private readonly ReadOnlySequence<byte> fragmentedSequence;
        private readonly ReadOnlySequence<byte> incompleteSequence;

        public SequenceExtensionsTryReadMqttStringShould()
        {
            completeSequence = new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66,
                0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0,
                0xb3, 0xd0, 0xb4, 0xd0, 0xb5
            });

            emptySequence = new ReadOnlySequence<byte>(Array.Empty<byte>());

            incompleteSequence = new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66,
                0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0
            });

            var segment1 = new Segment<byte>(new byte[] {0x00, 0x13, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66});
            var segment2 = segment1
                .Append(new byte[] {0x2d, 0xd0, 0xb0, 0xd0, 0xb1, 0xd0, 0xb2, 0xd0})
                .Append(new byte[] {0xb3, 0xd0, 0xb4, 0xd0, 0xb5});
            fragmentedSequence = new ReadOnlySequence<byte>(segment1, 0, segment2, 5);
        }

        [TestMethod]
        public void ReturnFalseGivenEmptySequence()
        {
            var actual = emptySequence.TryReadMqttString(out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenIncompleteSequence()
        {
            var actual = incompleteSequence.TryReadMqttString(out _, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenCompleteSequence()
        {
            const string expectedValue = "abcdef-абвгде";

            var actual = completeSequence.TryReadMqttString(out var actualValue, out var consumed);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
            Assert.AreEqual(21, consumed);
        }

        [TestMethod]
        public void ReturnTrueGivenFragmentedSequence()
        {
            const string expectedValue = "abcdef-абвгде";

            var actual = fragmentedSequence.TryReadMqttString(out var actualValue, out var consumed);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
            Assert.AreEqual(21, consumed);
        }
    }
}