using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.UnsubscribePacket;

[TestClass]
public class WriteShould
{
    private readonly Packets.V5.UnsubscribePacket unsubscribePacket = new(0x0002, new ReadOnlyMemory<byte>[] { "testtopic0/#"u8.ToArray(), "testtopic1/#"u8.ToArray(), "testtopic2/#"u8.ToArray() });

    [TestMethod]
    public void SetHeaderBytes_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = unsubscribePacket.Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual((byte)(0b1010_0000 | 0b0010), actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(45, actualRemainingLength);
    }

    [TestMethod]
    public void EncodePacketId_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = unsubscribePacket.Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        Assert.AreEqual((byte)0x0002, actualPacketId);
    }

    [TestMethod]
    public void EncodeTopics_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(47);
        var written = unsubscribePacket.Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(47, written);
        Assert.AreEqual(47, writer.WrittenCount);

        Assert.AreEqual(12, BinaryPrimitives.ReadUInt16BigEndian(bytes[5..]));
        Assert.IsTrue(bytes.Slice(7, 12).SequenceEqual("testtopic0/#"u8));

        Assert.AreEqual(12, BinaryPrimitives.ReadUInt16BigEndian(bytes[19..]));
        Assert.IsTrue(bytes.Slice(21, 12).SequenceEqual("testtopic1/#"u8));

        Assert.AreEqual(12, BinaryPrimitives.ReadUInt16BigEndian(bytes[33..]));
        Assert.IsTrue(bytes.Slice(35, 12).SequenceEqual("testtopic2/#"u8));
    }

    [TestMethod]
    public void EncodeUserProperties_GivenSampleMessage()
    {
        var writer = new ArrayBufferWriter<byte>(51);
        var written = new Packets.V5.UnsubscribePacket(0x0002, new ReadOnlyMemory<byte>[] { "testtopic0/#"u8.ToArray() })
        {
            Properties = new List<Utf8StringPair>()
            {
                ("prop1"u8.ToArray(), "value1"u8.ToArray()),
                ("prop2"u8.ToArray(), "value2"u8.ToArray())
            }
        }.Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(51, written);
        Assert.AreEqual(51, writer.WrittenCount);

        Assert.AreEqual(32, bytes[4]);

        Assert.AreEqual(0x26, bytes[5]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes[6..]));
        Assert.IsTrue(bytes.Slice(8, 5).SequenceEqual("prop1"u8));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes[13..]));
        Assert.IsTrue(bytes.Slice(15, 6).SequenceEqual("value1"u8));

        Assert.AreEqual(0x26, bytes[21]);
        Assert.AreEqual(5, BinaryPrimitives.ReadUInt16BigEndian(bytes[22..]));
        Assert.IsTrue(bytes.Slice(24, 5).SequenceEqual("prop2"u8));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16BigEndian(bytes[29..]));
        Assert.IsTrue(bytes.Slice(31, 6).SequenceEqual("value2"u8));
    }
}