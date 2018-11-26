﻿using System.Buffers;
using System.Net.Mqtt.Buffers;
using System.Net.Mqtt.Packets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.ConnectPacketTests
{
    [TestClass]
    public class ConnectPacketV4_TryParse_Should
    {
        private readonly ReadOnlySequence<byte> fragmentedSample;
        private readonly ReadOnlySequence<byte> invalidSizeFragmentedSample;

        private readonly byte[] invalidSizeSample =
        {
            0x10, 0x50, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xc6, 0x00, 0x78, 0x00, 0x0c, 0x54, 0x65,
            0x73, 0x74, 0x43, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x49, 0x64, 0x00, 0x0d, 0x54, 0x65, 0x73, 0x74,
            0x57, 0x69, 0x6c, 0x6c, 0x54, 0x6f, 0x70, 0x69, 0x63, 0x00, 0x0f, 0x54, 0x65, 0x73, 0x74, 0x57,
            0x69, 0x6c, 0x6c, 0x4d, 0x65, 0x73, 0x73, 0x61
        };

        private readonly ReadOnlySequence<byte> invalidTypeFragmentedSample;

        private readonly byte[] invalidTypeSample =
        {
            0x13, 0x50, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xc6, 0x00, 0x78, 0x00, 0x0c, 0x54, 0x65,
            0x73, 0x74, 0x43, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x49, 0x64, 0x00, 0x0d, 0x54, 0x65, 0x73, 0x74,
            0x57, 0x69, 0x6c, 0x6c, 0x54, 0x6f, 0x70, 0x69, 0x63, 0x00, 0x0f, 0x54, 0x65, 0x73, 0x74, 0x57,
            0x69, 0x6c, 0x6c, 0x4d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00, 0x08, 0x54, 0x65, 0x73, 0x74,
            0x55, 0x73, 0x65, 0x72, 0x00, 0x0c, 0x54, 0x65, 0x73, 0x74, 0x50, 0x61, 0x73, 0x73, 0x77, 0x6f,
            0x72, 0x64
        };

        private readonly byte[] noClientIdSample =
        {
            0x10, 0x44, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xf6, 0x00, 0x78, 0x00, 0x00, 0x00, 0x0d,
            0x54, 0x65, 0x73, 0x74, 0x57, 0x69, 0x6c, 0x6c, 0x54, 0x6f, 0x70, 0x69, 0x63, 0x00, 0x0f, 0x54,
            0x65, 0x73, 0x74, 0x57, 0x69, 0x6c, 0x6c, 0x4d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00, 0x08,
            0x54, 0x65, 0x73, 0x74, 0x55, 0x73, 0x65, 0x72, 0x00, 0x0c, 0x54, 0x65, 0x73, 0x74, 0x50, 0x61,
            0x73, 0x73, 0x77, 0x6f, 0x72, 0x64
        };

        private readonly byte[] noPasswordSample =
        {
            0x10, 0x16, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xb2, 0x00, 0x78, 0x00, 0x00, 0x00, 0x08,
            0x54, 0x65, 0x73, 0x74, 0x55, 0x73, 0x65, 0x72
        };

        private readonly byte[] noUserNameSample =
        {
            0x10, 0x0c, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0x32, 0x00, 0x78, 0x00, 0x00
        };

        private readonly byte[] noWillMessageSample =
        {
            0x10, 0x30, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xf2, 0x00, 0x78, 0x00, 0x0c, 0x54, 0x65,
            0x73, 0x74, 0x43, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x49, 0x64, 0x00, 0x08, 0x54, 0x65, 0x73, 0x74,
            0x55, 0x73, 0x65, 0x72, 0x00, 0x0c, 0x54, 0x65, 0x73, 0x74, 0x50, 0x61, 0x73, 0x73, 0x77, 0x6f,
            0x72, 0x64
        };

        private readonly byte[] sample =
        {
            0x10, 0x50, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xf6, 0x00, 0x78, 0x00, 0x0c, 0x54, 0x65,
            0x73, 0x74, 0x43, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x49, 0x64, 0x00, 0x0d, 0x54, 0x65, 0x73, 0x74,
            0x57, 0x69, 0x6c, 0x6c, 0x54, 0x6f, 0x70, 0x69, 0x63, 0x00, 0x0f, 0x54, 0x65, 0x73, 0x74, 0x57,
            0x69, 0x6c, 0x6c, 0x4d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00, 0x08, 0x54, 0x65, 0x73, 0x74,
            0x55, 0x73, 0x65, 0x72, 0x00, 0x0c, 0x54, 0x65, 0x73, 0x74, 0x50, 0x61, 0x73, 0x73, 0x77, 0x6f,
            0x72, 0x64
        };

        public ConnectPacketV4_TryParse_Should()
        {
            var segment1 = new Segment<byte>(new byte[]
            {
                0x10, 0x50, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xf6, 0x00, 0x78, 0x00, 0x0c, 0x54, 0x65,
                0x73, 0x74, 0x43, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x49, 0x64, 0x00, 0x0d, 0x54, 0x65, 0x73, 0x74
            });

            var segment2 = segment1
                .Append(new byte[]
                {
                    0x57, 0x69, 0x6c, 0x6c, 0x54, 0x6f, 0x70, 0x69, 0x63, 0x00, 0x0f, 0x54, 0x65, 0x73, 0x74, 0x57,
                    0x69, 0x6c, 0x6c, 0x4d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00, 0x08, 0x54, 0x65, 0x73, 0x74
                })
                .Append(new byte[]
                {
                    0x55, 0x73, 0x65, 0x72, 0x00, 0x0c, 0x54, 0x65, 0x73, 0x74, 0x50, 0x61, 0x73, 0x73, 0x77, 0x6f,
                    0x72, 0x64
                });

            fragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 18);

            segment1 = new Segment<byte>(new byte[]
            {
                0x13, 0x50, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xf6, 0x00, 0x78, 0x00, 0x0c, 0x54, 0x65,
                0x73, 0x74, 0x43, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x49, 0x64, 0x00, 0x0d, 0x54, 0x65, 0x73, 0x74
            });

            segment2 = segment1
                .Append(new byte[]
                {
                    0x57, 0x69, 0x6c, 0x6c, 0x54, 0x6f, 0x70, 0x69, 0x63, 0x00, 0x0f, 0x54, 0x65, 0x73, 0x74, 0x57,
                    0x69, 0x6c, 0x6c, 0x4d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00, 0x08, 0x54, 0x65, 0x73, 0x74
                })
                .Append(new byte[]
                {
                    0x55, 0x73, 0x65, 0x72, 0x00, 0x0c, 0x54, 0x65, 0x73, 0x74, 0x50, 0x61, 0x73, 0x73, 0x77, 0x6f,
                    0x72, 0x64
                });

            invalidTypeFragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 18);

            segment1 = new Segment<byte>(new byte[]
            {
                0x13, 0x50, 0x00, 0x04, 0x4d, 0x51, 0x54, 0x54, 0x04, 0xf6, 0x00, 0x78, 0x00, 0x0c, 0x54, 0x65,
                0x73, 0x74, 0x43, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x49, 0x64, 0x00, 0x0d, 0x54, 0x65, 0x73, 0x74
            });

            segment2 = segment1
                .Append(new byte[]
                {
                    0x57, 0x69, 0x6c, 0x6c, 0x54, 0x6f, 0x70, 0x69, 0x63, 0x00, 0x0f, 0x54, 0x65, 0x73, 0x74, 0x57,
                    0x69, 0x6c, 0x6c, 0x4d, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00, 0x08, 0x54, 0x65, 0x73, 0x74
                })
                .Append(new byte[]
                {
                    0x55, 0x73, 0x65, 0x72, 0x00, 0x0c, 0x54, 0x65, 0x73, 0x74, 0x50, 0x61, 0x73
                });

            invalidSizeFragmentedSample = new ReadOnlySequence<byte>(segment1, 0, segment2, 13);
        }

        [TestMethod]
        public void ReturnTrue_AndValidPacket_GivenSample()
        {
            var actual = ConnectPacketV4.TryParse(sample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual("MQTT", packet.ProtocolName);
            Assert.AreEqual(0x04, packet.ProtocolLevel);
            Assert.AreEqual(2, packet.WillQoS);
            Assert.IsTrue(packet.WillRetain);
            Assert.IsTrue(packet.CleanSession);
            Assert.AreEqual(120, packet.KeepAlive);
            Assert.AreEqual("TestClientId", packet.ClientId);
            Assert.AreEqual("TestWillTopic", packet.WillTopic);
            Assert.AreEqual("TestWillMessage", Encoding.UTF8.GetString(packet.WillMessage.Span));
            Assert.AreEqual("TestUser", packet.UserName);
            Assert.AreEqual("TestPassword", packet.Password);
        }

        [TestMethod]
        public void ReturnTrue_AndValidPacket_GivenFragmentedSample()
        {
            var actual = ConnectPacketV4.TryParse(fragmentedSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.AreEqual("MQTT", packet.ProtocolName);
            Assert.AreEqual(0x04, packet.ProtocolLevel);
            Assert.AreEqual(2, packet.WillQoS);
            Assert.IsTrue(packet.WillRetain);
            Assert.IsTrue(packet.CleanSession);
            Assert.AreEqual(120, packet.KeepAlive);
            Assert.AreEqual("TestClientId", packet.ClientId);
            Assert.AreEqual("TestWillTopic", packet.WillTopic);
            Assert.AreEqual("TestWillMessage", Encoding.UTF8.GetString(packet.WillMessage.Span));
            Assert.AreEqual("TestUser", packet.UserName);
            Assert.AreEqual("TestPassword", packet.Password);
        }

        [TestMethod]
        public void ReturnTrue_AndPacketNotNull_WillTopicNull_GivenSampleWithNoWillMessage()
        {
            var actual = ConnectPacketV4.TryParse(noWillMessageSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.IsNull(packet.WillTopic);
            Assert.AreEqual(0, packet.WillMessage.Length);
        }

        [TestMethod]
        public void ReturnTrue_AndPacketNotNull_ClientIdNull_GivenSampleWithNoClientId()
        {
            var actual = ConnectPacketV4.TryParse(noClientIdSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.IsNull(packet.ClientId);
        }

        [TestMethod]
        public void ReturnTrue_AndPacketNotNull_UserNameNull_GivenSampleWithNoUserName()
        {
            var actual = ConnectPacketV4.TryParse(noUserNameSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.IsNull(packet.UserName);
        }

        [TestMethod]
        public void ReturnTrue_AndPacketNotNull_PasswordNull_GivenSampleWithNoPassword()
        {
            var actual = ConnectPacketV4.TryParse(noPasswordSample, out var packet);

            Assert.IsTrue(actual);
            Assert.IsNotNull(packet);
            Assert.IsNull(packet.Password);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketNull_GivenInvalidTypeSample()
        {
            var actual = ConnectPacketV4.TryParse(invalidTypeSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketNull_GivenInvalidTypeFragmentedSample()
        {
            var actual = ConnectPacketV4.TryParse(invalidTypeFragmentedSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketNull_GivenInvalidSizeSample()
        {
            var actual = ConnectPacketV4.TryParse(invalidSizeSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public void ReturnFalse_AndPacketNull_GivenInvalidSizeFragmentedSample()
        {
            var actual = ConnectPacketV4.TryParse(invalidSizeFragmentedSample, out var packet);

            Assert.IsFalse(actual);
            Assert.IsNull(packet);
        }
    }
}