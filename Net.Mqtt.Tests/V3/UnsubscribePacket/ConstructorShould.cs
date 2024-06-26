﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Mqtt.Tests.V3.UnsubscribePacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        var _ = new Packets.V3.UnsubscribePacket(0, ["topic1"u8.ToArray()]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ThrowArgumentNullExceptionGivenTopicsNull()
    {
        var _ = new Packets.V3.UnsubscribePacket(1, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionGivenTopicsEmpty()
    {
        var _ = new Packets.V3.UnsubscribePacket(1, []);
    }
}