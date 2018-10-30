using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.MqttHelpersTests
{
    [TestClass]
    public class MqttHelpers_GetLengthByteCount_Should
    {
        [TestMethod]
        public void Return1GivenValueOf0()
        {
            Assert.AreEqual(1, MqttHelpers.GetLengthByteCount(0));
        }

        [TestMethod]
        public void Return1GivenValueOf100()
        {
            Assert.AreEqual(1, MqttHelpers.GetLengthByteCount(100));
        }

        [TestMethod]
        public void Return1GivenValueOf127()
        {
            Assert.AreEqual(1, MqttHelpers.GetLengthByteCount(127));
        }

        [TestMethod]
        public void Return2GivenValueOf128()
        {
            Assert.AreEqual(2, MqttHelpers.GetLengthByteCount(128));
        }

        [TestMethod]
        public void Return2GivenValueOf16000()
        {
            Assert.AreEqual(2, MqttHelpers.GetLengthByteCount(16000));
        }

        [TestMethod]
        public void Return2GivenValueOf16383()
        {
            Assert.AreEqual(2, MqttHelpers.GetLengthByteCount(16383));
        }

        [TestMethod]
        public void Return3GivenValueOf16384()
        {
            Assert.AreEqual(3, MqttHelpers.GetLengthByteCount(16384));
        }

        [TestMethod]
        public void Return3GivenValueOf2097000()
        {
            Assert.AreEqual(3, MqttHelpers.GetLengthByteCount(2097000));
        }

        [TestMethod]
        public void Return3GivenValueOf2097151()
        {
            Assert.AreEqual(3, MqttHelpers.GetLengthByteCount(2097151));
        }

        [TestMethod]
        public void Return4GivenValueOf2097152()
        {
            Assert.AreEqual(4, MqttHelpers.GetLengthByteCount(2097152));
        }

        [TestMethod]
        public void Return4GivenValueOf268435000()
        {
            Assert.AreEqual(4, MqttHelpers.GetLengthByteCount(268435000));
        }

        [TestMethod]
        public void Return4GivenValueOf268435455()
        {
            Assert.AreEqual(4, MqttHelpers.GetLengthByteCount(268435455));
        }
    }
}