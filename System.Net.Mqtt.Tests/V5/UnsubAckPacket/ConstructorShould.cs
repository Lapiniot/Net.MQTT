﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.UnsubAckPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        var _ = new Packets.V5.UnsubAckPacket(0, [0]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ThrowArgumentNullExceptionGivenResultParamNull()
    {
        var _ = new Packets.V5.UnsubAckPacket(1, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenResultParamEmpty()
    {
        var _ = new Packets.V5.UnsubAckPacket(1, Array.Empty<byte>());
    }
}