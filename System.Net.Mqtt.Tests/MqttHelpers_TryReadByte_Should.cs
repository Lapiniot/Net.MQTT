using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests
{
    [TestClass]
    public class MqttHelpers_TryReadByte_Should
    {
        private readonly ReadOnlySequence<byte> completeSequence;
        private readonly ReadOnlySequence<byte> emptySequence;
        private readonly ReadOnlySequence<byte> fragmentedSequence;

        public MqttHelpers_TryReadByte_Should()
        {
            completeSequence = new ReadOnlySequence<byte>(new byte[] {0x40});
            emptySequence = new ReadOnlySequence<byte>(new byte[0]);
            var segment = new Segment<byte>(new byte[0]);
            fragmentedSequence = new ReadOnlySequence<byte>(segment, 0, segment.Append(new byte[] {0x40}), 1);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptySequence()
        {
            var actual = MqttHelpers.TryReadByte(emptySequence, out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenCompleteSequence()
        {
            var expectedValue = 0x40;

            var actual = MqttHelpers.TryReadByte(completeSequence, out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void ReturnTrue_GivenFragmentedSequence()
        {
            var expectedValue = 0x40;

            var actual = MqttHelpers.TryReadByte(fragmentedSequence, out var actualValue);

            Assert.IsTrue(actual);
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}