﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.PublishPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenTopicNull()
    {
        var _ = new Packets.V5.PublishPacket(0, default, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenTopicEmpty()
    {
        var _ = new Packets.V5.PublishPacket(0, default, default);
    }

    [TestMethod]
    public void NotThrowArgumentExceptionGivenQoS0AndNoPacketId()
    {
        var _ = new Packets.V5.PublishPacket(0, 0, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS1AndNoPacketId()
    {
        var _ = new Packets.V5.PublishPacket(0, 1, "/"u8.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenQoS2AndNoPacketId()
    {
        var _ = new Packets.V5.PublishPacket(0, 2, "/"u8.ToArray());
    }
}