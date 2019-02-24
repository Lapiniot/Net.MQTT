using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.ExtensionsTests
{
    [TestClass]
    public class SpanExtensions_GetLengthByteCount_Should
    {
        [TestMethod]
        public void Return1GivenValueOf0()
        {
            Assert.AreEqual(1, MqttExtensions.GetLengthByteCount(0));
        }

        [TestMethod]
        public void Return1GivenValueOf100()
        {
            Assert.AreEqual(1, MqttExtensions.GetLengthByteCount(100));
        }

        [TestMethod]
        public void Return1GivenValueOf127()
        {
            Assert.AreEqual(1, MqttExtensions.GetLengthByteCount(127));
        }

        [TestMethod]
        public void Return2GivenValueOf128()
        {
            Assert.AreEqual(2, MqttExtensions.GetLengthByteCount(128));
        }

        [TestMethod]
        public void Return2GivenValueOf16000()
        {
            Assert.AreEqual(2, MqttExtensions.GetLengthByteCount(16000));
        }

        [TestMethod]
        public void Return2GivenValueOf16383()
        {
            Assert.AreEqual(2, MqttExtensions.GetLengthByteCount(16383));
        }

        [TestMethod]
        public void Return3GivenValueOf16384()
        {
            Assert.AreEqual(3, MqttExtensions.GetLengthByteCount(16384));
        }

        [TestMethod]
        public void Return3GivenValueOf2097000()
        {
            Assert.AreEqual(3, MqttExtensions.GetLengthByteCount(2097000));
        }

        [TestMethod]
        public void Return3GivenValueOf2097151()
        {
            Assert.AreEqual(3, MqttExtensions.GetLengthByteCount(2097151));
        }

        [TestMethod]
        public void Return4GivenValueOf2097152()
        {
            Assert.AreEqual(4, MqttExtensions.GetLengthByteCount(2097152));
        }

        [TestMethod]
        public void Return4GivenValueOf268435000()
        {
            Assert.AreEqual(4, MqttExtensions.GetLengthByteCount(268435000));
        }

        [TestMethod]
        public void Return4GivenValueOf268435455()
        {
            Assert.AreEqual(4, MqttExtensions.GetLengthByteCount(268435455));
        }
    }
}