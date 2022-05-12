﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.ConnectPacket;

[TestClass]
public class GetHeaderSizeShould
{
    [TestMethod]
    public void Return12GivenMessageWithProtocolV3()
    {
        var m = new Packets.ConnectPacket((byte[])"test-client-id", 0x03, (byte[])"MQIsdp");
        const int expected = 12;
        var actual = m.HeaderSize;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Return10GivenMessageWithProtocolV4()
    {
        var m = new Packets.ConnectPacket((byte[])"test-client-id", 0x04, (byte[])"MQTT");
        const int expected = 10;
        var actual = m.HeaderSize;
        Assert.AreEqual(expected, actual);
    }
}