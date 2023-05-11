﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.SubAckPacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ThrowArgumentOutOfRangeExceptionGivenPacketId0()
    {
        var _ = new Packets.V5.SubAckPacket(0, new byte[] { 0 });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ThrowArgumentNullExceptionGivenResultParamNull()
    {
        var _ = new Packets.V5.SubAckPacket(1, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenResultParamEmpty()
    {
        var _ = new Packets.V5.SubAckPacket(1, Array.Empty<byte>());
    }
}