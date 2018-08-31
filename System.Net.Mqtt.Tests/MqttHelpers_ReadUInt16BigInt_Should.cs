using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class MqttHelpers_TryReadUInt16_Should
    {
        private readonly ReadOnlySequence<byte> completeSequence;
        private readonly ReadOnlySequence<byte> emptySequence;
        private readonly ReadOnlySequence<byte> fragmentedSequence;
        private readonly ReadOnlySequence<byte> incompleteSequence;

        public MqttHelpers_TryReadUInt16_Should()
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
            var actual = MqttHelpers.TryReadUInt16(emptySequence, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenIncompleteSequence()
        {
            var actual = MqttHelpers.TryReadUInt16(incompleteSequence, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenCompleteSequence()
        {
            var expectedValue = 0x40cd;

            var actual = MqttHelpers.TryReadUInt16(completeSequence, out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void ReturnTrue_GivenFragmentedSequence()
        {
            var expectedValue = 0x40FF;

            var actual = MqttHelpers.TryReadUInt16(fragmentedSequence, out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}