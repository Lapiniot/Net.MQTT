﻿using System.Buffers;
using System.Memory;
using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.ExtensionsTests
{
    [TestClass]
    public class SequenceReaderExtensions_TryReadMqttString_Should
    {
        [TestMethod]
        public void ReturnTrueAndReadString_GivenContiguousSequenceFromStart()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0,
                0xb5, 0xd1, 0x81, 0xd1, 0x82
            }));

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsTrue(actual);
            Assert.AreEqual("TestString-Тест", value);
            Assert.AreEqual(21, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueAndReadString_GivenContiguousSequence()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03,
                0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0,
                0xb5, 0xd1, 0x81, 0xd1, 0x82
            }));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);


            Assert.IsTrue(actual);
            Assert.AreEqual("TestString-Тест", value);
            Assert.AreEqual(29, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueAndReadString_GivenFragmentedSequenceType1()
        {
            var start = new Segment<byte>(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03,
                0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0,
                0xb5, 0xd1, 0x81, 0xd1, 0x82
            });
            var end = start.Append(new byte[] {0x00, 0x01, 0x02, 0x03});
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(start, 0, end, 4));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);


            Assert.IsTrue(actual);
            Assert.AreEqual("TestString-Тест", value);
            Assert.AreEqual(29, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueAndReadString_GivenFragmentedSequenceType2()
        {
            var start = new Segment<byte>(new byte[] {0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03});
            var end = start.Append(new byte[]
                {
                    0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                    0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0,
                    0xb5, 0xd1, 0x81, 0xd1, 0x82
                })
                .Append(new byte[] {0x00, 0x01, 0x02, 0x03});
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(start, 0, end, 4));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsTrue(actual);
            Assert.AreEqual("TestString-Тест", value);
            Assert.AreEqual(29, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueAndReadString_GivenFragmentedSequenceType3()
        {
            var start = new Segment<byte>(new byte[] {0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00, 0x13});
            var end = start.Append(new byte[]
                {
                    0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                    0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0,
                    0xb5, 0xd1, 0x81, 0xd1, 0x82
                })
                .Append(new byte[] {0x00, 0x01, 0x02, 0x03});
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(start, 0, end, 4));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsTrue(actual);
            Assert.AreEqual("TestString-Тест", value);
            Assert.AreEqual(29, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueAndReadString_GivenFragmentedSequenceType4()
        {
            var start = new Segment<byte>(new byte[] {0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00});
            var end = start.Append(new byte[]
                {
                    0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                    0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0,
                    0xb5, 0xd1, 0x81, 0xd1, 0x82
                })
                .Append(new byte[] {0x00, 0x01, 0x02, 0x03});
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(start, 0, end, 4));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsTrue(actual);
            Assert.AreEqual("TestString-Тест", value);
            Assert.AreEqual(29, reader.Consumed);
        }

        [TestMethod]
        public void ReturnTrueAndReadString_GivenFragmentedSequenceType5()
        {
            var start = new Segment<byte>(new byte[] {0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00});
            var end = start.Append(new byte[] {0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74})
                .Append(new byte[] {0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0, 0xb5, 0xd1, 0x81, 0xd1, 0x82})
                .Append(new byte[] {0x00, 0x01, 0x02, 0x03});
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(start, 0, end, 4));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsTrue(actual);
            Assert.AreEqual("TestString-Тест", value);
            Assert.AreEqual(29, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseAndNullString_GivenEmptySequence()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] {}));

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsFalse(actual);
            Assert.IsNull(value);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseAndNullString_GivenContiguousSequenceTooShort()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] {0x00, 0x02}));

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsFalse(actual);
            Assert.IsNull(value);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseAndNullString_GivenContiguousIncompleteSequenceFromStart()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0
            }));

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsFalse(actual);
            Assert.IsNull(value);
            Assert.AreEqual(0, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseAndNullString_GivenContiguousIncompleteSequence()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03,
                0x00, 0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74,
                0x72, 0x69, 0x6e, 0x67, 0x2d, 0xd0, 0xa2, 0xd0
            }));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsFalse(actual);
            Assert.IsNull(value);
            Assert.AreEqual(8, reader.Consumed);
        }

        [TestMethod]
        public void ReturnFalseAndNullString_GivenFragmentedIncompleteSequence()
        {
            var start = new Segment<byte>(new byte[] {0x00, 0x01, 0x02, 0x03, 0x00, 0x01, 0x02, 0x03, 0x00});
            var end = start.Append(new byte[] {0x13, 0x54, 0x65, 0x73, 0x74, 0x53, 0x74}).Append(new byte[] {0x72, 0x69, 0x6e, 0x67});
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(start, 0, end, 4));
            reader.Advance(8);

            var actual = reader.TryReadMqttString(out var value);

            Assert.IsFalse(actual);
            Assert.IsNull(value);
            Assert.AreEqual(8, reader.Consumed);
        }
    }
}