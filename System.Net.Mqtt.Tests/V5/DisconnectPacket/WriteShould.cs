using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.V5.DisconnectPacket;

[TestClass]
public class WriteShould
{
    [TestMethod]
    public void SetHeaderBytes_OmitReasonCode_GivenDefaultReasonCodeAndNoPropertiesSample()
    {
        var writer = new ArrayBufferWriter<byte>(2);
        var written = new Packets.V5.DisconnectPacket(0x00).Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(2, written);
        Assert.AreEqual(2, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(0b1110_0000, actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(0, actualRemainingLength);
    }

    [TestMethod]
    public void SetHeaderBytes_EncodeReasonCode_GivenNonDefaultReasonCodeAndNoPropertiesSample()
    {
        var writer = new ArrayBufferWriter<byte>(3);
        var written = new Packets.V5.DisconnectPacket(0x04).Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(3, written);
        Assert.AreEqual(3, writer.WrittenCount);

        var actualHeaderFlags = bytes[0];
        Assert.AreEqual(0b1110_0000, actualHeaderFlags);

        var actualRemainingLength = bytes[1];
        Assert.AreEqual(1, actualRemainingLength);

        var actualReasonCode = bytes[2];
        Assert.AreEqual(0x04, actualReasonCode);
    }

    [TestMethod]
    public void EncodeSessionExpiryInterval()
    {
        var writer = new ArrayBufferWriter<byte>(9);
        var written = new Packets.V5.DisconnectPacket(0x04) { SessionExpiryInterval = 300 }
            .Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(9, written);
        Assert.AreEqual(9, writer.WrittenCount);

        Assert.IsTrue(bytes.Slice(3).SequenceEqual(new byte[] { 0x05, 0x11, 0x00, 0x00, 0x01, 0x2c }));
    }

    [TestMethod]
    public void EncodeReasonString()
    {
        var writer = new ArrayBufferWriter<byte>(24);
        var written = new Packets.V5.DisconnectPacket(0x04) { ReasonString = "Normal disconnect"u8.ToArray() }
            .Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(24, written);
        Assert.AreEqual(24, writer.WrittenCount);

        Assert.IsTrue(bytes.Slice(3).SequenceEqual(new byte[] {
            0x14, 0x1f, 0x00, 0x11, 0x4e, 0x6f, 0x72, 0x6d, 0x61, 0x6c, 0x20,
            0x64, 0x69, 0x73, 0x63, 0x6f, 0x6e, 0x6e, 0x65, 0x63, 0x74 }));
    }

    [TestMethod]
    public void EncodeServerReference()
    {
        var writer = new ArrayBufferWriter<byte>(21);
        var written = new Packets.V5.DisconnectPacket(0x04) { ServerReference = "another-server"u8.ToArray() }
            .Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(21, written);
        Assert.AreEqual(21, writer.WrittenCount);

        Assert.IsTrue(bytes.Slice(3).SequenceEqual(new byte[] {
            0x11, 0x1c, 0x00, 0x0e, 0x61, 0x6e, 0x6f, 0x74, 0x68,
            0x65, 0x72, 0x2d, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72 }));
    }

    [TestMethod]
    public void EncodeProperties()
    {
        var writer = new ArrayBufferWriter<byte>(68);
        var written = new Packets.V5.DisconnectPacket(0x04)
        {
            Properties = new List<(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>)>()
            {
                new("user-prop-1"u8.ToArray(),"user-prop1-value"u8.ToArray()),
                new("user-prop-2"u8.ToArray(),"user-prop2-value"u8.ToArray())
            }
        }.Write(writer, int.MaxValue, out var bytes);

        Assert.AreEqual(68, written);
        Assert.AreEqual(68, writer.WrittenCount);

        Assert.IsTrue(bytes.Slice(3).SequenceEqual(new byte[] {
            0x40, 0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72,
            0x6f, 0x70, 0x2d, 0x31, 0x00, 0x10, 0x75, 0x73, 0x65, 0x72, 0x2d,
            0x70, 0x72, 0x6f, 0x70, 0x31, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65,
            0x26, 0x00, 0x0b, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70, 0x72, 0x6f,
            0x70, 0x2d, 0x32, 0x00, 0x10, 0x75, 0x73, 0x65, 0x72, 0x2d, 0x70,
            0x72, 0x6f, 0x70, 0x32, 0x2d, 0x76, 0x61, 0x6c, 0x75, 0x65 }));
    }
}
