﻿using System.Buffers.Binary;
using System.Net.Mqtt.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.SubAckPacketTests
{
    [TestClass]
    public class SubAckPacket_Write_Should
    {
        private readonly SubAckPacket samplePacket = new SubAckPacket(0x02, new byte[] {1, 0, 2});

        [TestMethod]
        public void SetHeaderBytes_0x90_0x05_GivenSampleMessage()
        {
            var bytes = new byte[7];
            samplePacket.Write(bytes, 5);

            var expectedHeaderFlags = 0b1001_0000;
            var actualHeaderFlags = bytes[0];
            Assert.AreEqual(expectedHeaderFlags, actualHeaderFlags);

            var expectedRemainingLength = 0x05;
            var actualRemainingLength = bytes[1];
            Assert.AreEqual(expectedRemainingLength, actualRemainingLength);
        }

        [TestMethod]
        public void EncodePacketId_0x0002_GivenSampleMessage()
        {
            Span<byte> bytes = new byte[7];
            samplePacket.Write(bytes, 5);

            byte expectedPacketId = 0x0002;
            var actualPacketId = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2));
            Assert.AreEqual(expectedPacketId, actualPacketId);
        }

        [TestMethod]
        public void EncodeResultBytes_GivenSampleMessage()
        {
            var bytes = new byte[7];
            samplePacket.Write(bytes, 5);

            Assert.AreEqual(1, bytes[4]);
            Assert.AreEqual(0, bytes[5]);
            Assert.AreEqual(2, bytes[6]);
        }
    }
}