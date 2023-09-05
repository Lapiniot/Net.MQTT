﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.UnsubscribePacket;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenPacketId0()
    {
        var _ = new Packets.V5.UnsubscribePacket(0, new ReadOnlyMemory<byte>[] { "topic1"u8.ToArray() });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ThrowArgumentNullExceptionGivenTopicsNull()
    {
        var _ = new Packets.V5.UnsubscribePacket(1, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ThrowArgumentExceptionGivenTopicsEmpty()
    {
        var _ = new Packets.V5.UnsubscribePacket(1, Array.Empty<ReadOnlyMemory<byte>>());
    }
}