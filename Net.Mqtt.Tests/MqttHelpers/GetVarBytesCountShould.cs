using static Net.Mqtt.MqttHelpers;

namespace Net.Mqtt.Tests.MqttHelpers;

[TestClass]
public class GetVarBytesCountShould
{
    [TestMethod]
    public void Return1GivenValueOf0() => Assert.AreEqual(1, GetVarBytesCount(0));

    [TestMethod]
    public void Return1GivenValueOf100() => Assert.AreEqual(1, GetVarBytesCount(100));

    [TestMethod]
    public void Return1GivenValueOf127() => Assert.AreEqual(1, GetVarBytesCount(127));

    [TestMethod]
    public void Return2GivenValueOf128() => Assert.AreEqual(2, GetVarBytesCount(128));

    [TestMethod]
    public void Return2GivenValueOf16000() => Assert.AreEqual(2, GetVarBytesCount(16000));

    [TestMethod]
    public void Return2GivenValueOf16383() => Assert.AreEqual(2, GetVarBytesCount(16383));

    [TestMethod]
    public void Return3GivenValueOf16384() => Assert.AreEqual(3, GetVarBytesCount(16384));

    [TestMethod]
    public void Return3GivenValueOf2097000() => Assert.AreEqual(3, GetVarBytesCount(2097000));

    [TestMethod]
    public void Return3GivenValueOf2097151() => Assert.AreEqual(3, GetVarBytesCount(2097151));

    [TestMethod]
    public void Return4GivenValueOf2097152() => Assert.AreEqual(4, GetVarBytesCount(2097152));

    [TestMethod]
    public void Return4GivenValueOf268435000() => Assert.AreEqual(4, GetVarBytesCount(268435000));

    [TestMethod]
    public void Return4GivenValueOf268435455() => Assert.AreEqual(4, GetVarBytesCount(268435455));
}