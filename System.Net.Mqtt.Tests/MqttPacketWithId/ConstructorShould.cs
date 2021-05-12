using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.MqttPacketWithId
{
    [TestClass]
    public class ConstructorShould
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowArgumentOutOfRangeExceptionGivenPacketId0()
        {
            _ = new UnsubAckPacket(0);
        }
    }
}