using System.Buffers.Binary;
using System.Net.Mqtt.Packets.V5;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.PubRecPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void WriteHeader_PacketId_NoReasonCode_GivenDefaultReasonCode()
    {
        var writer = new ArrayBufferWriter<byte>(4);

        var written = new Packets.V5.PubRecPacket(16).Write(writer, int.MaxValue);
        var buffer = writer.WrittenSpan;

        Assert.AreEqual(4, written);
        Assert.AreEqual(4, writer.WrittenCount);

        Assert.AreEqual(PacketFlags.PubRecMask, buffer[0]);
        Assert.AreEqual(2, buffer[1]);
        Assert.AreEqual(16, BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2)));
    }

    [TestMethod]
    public void WriteHeader_PacketId_ReasonCode_GivenDefaultReasonCode()
    {
        var writer = new ArrayBufferWriter<byte>(5);

        var written = new Packets.V5.PubRecPacket(16, ReasonCode.UnspecifiedError).Write(writer, int.MaxValue);
        var buffer = writer.WrittenSpan;

        Assert.AreEqual(5, written);
        Assert.AreEqual(5, writer.WrittenCount);

        Assert.AreEqual(PacketFlags.PubRecMask, buffer[0]);
        Assert.AreEqual(3, buffer[1]);
        Assert.AreEqual(16, BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2)));
        Assert.AreEqual(ReasonCode.UnspecifiedError, (ReasonCode)buffer[4]);
    }
}