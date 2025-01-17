using System.Runtime.CompilerServices;
using static Net.Mqtt.TopicHelpers;

namespace Net.Mqtt.Tests.TopicHelpers;

[TestClass]
public class FirstSegmentLengthShould
{
    [TestMethod]
    public void ReturnZeroGivenZeroLength()
    {
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in "abc"u8[0]), 0);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    [Description("""
        Test for potential buffer overflow issue related to signed/unsigned conversion like this:
        for (; i < (nuint)length; i++)
        {
            if (Unsafe.AddByteOffset(ref source, i) == v)
                break;
        }
    """)]
    public void ReturnZeroGivenNegativeLength_TestPotentialOvervlow_SignedUnsignedConversion()
    {
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in "abcdefg/"u8[0]), -1);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    [Description("""
        Test for potential buffer overflow issue related to signed integer overflow doing math like this:
        for (; (nint)i <= length - 4; i += 4)
        {
            if (Unsafe.AddByteOffset(ref source, i + 0) == separator) return (int)i + 0;
            if (Unsafe.AddByteOffset(ref source, i + 1) == separator) return (int)i + 1;
            if (Unsafe.AddByteOffset(ref source, i + 2) == separator) return (int)i + 2;
            if (Unsafe.AddByteOffset(ref source, i + 3) == separator) return (int)i + 3;
        }
    """)]
    public void ReturnZeroGivenNegativeLength_TestPotentialOvervlow_SignedSubstructionOverflow()
    {
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in "abcdefg/"u8[0]), int.MinValue);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    public void ReturnLengthGivenSourceWithDelimiterAfterLength()
    {
        var source = "abcde/cde"u8;
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), 3);
        Assert.AreEqual(3, actual);
    }

    [TestMethod]
    public void ReturnFirstDelimiterPositionGivenSource_HitsUnrolledLoop()
    {
        var source = "aaaabbbbcccc"u8;
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(12, actual);

        source = "aaaabbbbccc/"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(11, actual);

        source = "/aaaabbbbcccc"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(0, actual);

        source = "aaaa/bbbbcccc"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(4, actual);

        source = "aaaabb/bbcccc"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(6, actual);
    }

    [TestMethod]
    public void ReturnFirstDelimiterPositionGivenSource_HitsSimpleLoop()
    {
        var source = "abc"u8;
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(3, actual);

        source = "/ab"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(0, actual);

        source = "ab/"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(2, actual);

        source = "a/b"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(1, actual);

        source = "/"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    public void ReturnFirstDelimiterPositionGivenSource_HitsBothLoops()
    {
        var source = "aabbccddee"u8;
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(10, actual);

        source = "aabbccdd/ee"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(8, actual);

        source = "aabbccdde/e"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(9, actual);

        source = "aabbccddee/"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(10, actual);
    }

    [TestMethod]
    public void ReturnFirstDelimiterPositionGivenSource_HitsVector256Loop()
    {
        var source = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(64, actual);

        source = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhh/"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(63, actual);

        source = "/aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(0, actual);

        source = "aaaaaaaa/bbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(8, actual);

        source = "aaaaaaaabbbbbbbb/ccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(16, actual);

        source = "aaaaaaaabbbbbbbbcccccccc/ddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(24, actual);

        source = "aaaaaaaabbbbbbbbccccccccddddddd/eeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(31, actual);
    }

    [TestMethod]
    public void ReturnFirstDelimiterPositionGivenSource_HitsVector128Loop()
    {
        var source = "aaaabbbbccccdddd"u8;
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(16, actual);

        source = "/aaaabbbbccccdddd"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(0, actual);

        source = "aaaabbbbccccddd/"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(15, actual);

        source = "aaaabbbb/ccccdddd"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(8, actual);

        source = "aaaa/bbbbccccdddd"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(4, actual);

        source = "aaaabbbbcccc/dddd"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(12, actual);
    }

    [TestMethod]
    public void ReturnFirstDelimiterPositionGivenSource_HitsAllLoops()
    {
        var source = "aaaaaaaabbbbbbbbccccccccddddddddeeeeffffgggghhhhiiiijj"u8;
        var actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(54, actual);

        source = "aaaaaaaabbbbbbbbccccccccddddddddeeeeffffgggghhhhiiiijj/"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(54, actual);

        source = "aaaaaaaabbbbbbbbccccccccddddddddeeeeffffgggghhhhiiii/jj"u8;
        actual = FirstSegmentLength(ref Unsafe.AsRef(in source[0]), source.Length);
        Assert.AreEqual(52, actual);
    }
}