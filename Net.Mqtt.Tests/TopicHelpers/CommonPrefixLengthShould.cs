using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Net.Mqtt.TopicHelpers;

namespace Net.Mqtt.Tests.TopicHelpers;

[TestClass]
public class CommonPrefixLengthShould
{
    [TestMethod]
    public void ReturnZeroGivenZeroLength()
    {
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in "abc"u8[0]), ref Unsafe.AsRef(in "def"u8[0]), 0);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    [Description("""
        Consider edge case if we have code like this:

        for (; i < (nuint)length; i++)
        {
            if (Unsafe.AddByteOffset(ref left, i) != Unsafe.AddByteOffset(ref right, i))
                break;
        }
        
        Negative -1 becomes positive 0xffffffff after signed/unsigned casting and we wrongly 
        enter the loop due to overflow. This test expects CommonPrefixLength to return 0 
        rather than 7 as it likely would be in case of overflow is not detected promptly 
        right before entering scan loop
    """)]
    public void ReturnZeroGivenNegativeLength_TestPotentialOvervlow_SignedUnsignedConversion()
    {
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in "abcdefg0"u8[0]), ref Unsafe.AsRef(in "abcdefg1"u8[0]), -1);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    [Description("""
        Consider edge case if we have code like this:

        for (; (nint)i <= length - 4; i += 4)
        {
            if (Unsafe.AddByteOffset(ref left, i + 0) != Unsafe.AddByteOffset(ref right, i + 0)) return (int)i + 0;
            if (Unsafe.AddByteOffset(ref left, i + 1) != Unsafe.AddByteOffset(ref right, i + 0)) return (int)i + 1;
            if (Unsafe.AddByteOffset(ref left, i + 2) != Unsafe.AddByteOffset(ref right, i + 0)) return (int)i + 2;
            if (Unsafe.AddByteOffset(ref left, i + 3) != Unsafe.AddByteOffset(ref right, i + 0)) return (int)i + 3;
        }
        
        Negative -2147483648 becomes large positive 2147483644 after substruction, 
        and we wrongly enter the loop due to this overflow. This test expects CommonPrefixLength to return 0 
        rather than 7 as it likely would be in case of overflow is not detected promptly 
        right before entering scan loop
    """)]
    public void ReturnZeroGivenNegativeLength_TestPotentialOvervlow_SignedSubstructionOverflow()
    {
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in "abcdefg0"u8[0]), ref Unsafe.AsRef(in "abcdefg1"u8[0]), int.MinValue);
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsUnrolledLoop()
    {
        var left = "aaaabbbbcccc"u8;
        var right = "aaaabbbbcccc"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(12, actual);

        left = "aaaabbbbccc1"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(11, actual);

        left = "aaaabbbbcc1c"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(10, actual);

        left = "aaaabbbbc1cc"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(9, actual);

        left = "aaaabbbb1ccc"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(8, actual);

        left = "1aaabbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);

        left = "a1aabbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(1, actual);

        left = "aa1abbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(2, actual);

        left = "aaa1bbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(3, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsSimpleLoop()
    {
        var left = "abc"u8;
        var right = "abc"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(3, actual);

        left = "abc"u8;
        right = "ab1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(2, actual);

        left = "abc"u8;
        right = "a1c"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(1, actual);

        left = "abc"u8;
        right = "1bc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);

        left = "a"u8;
        right = "b"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsBothLoops()
    {
        var left = "aabbccddeef"u8;
        var right = "aabbccddeef"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(11, actual);

        left = "aabbccddeef"u8;
        right = "aabbccddee1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(10, actual);

        left = "aabbccddeef"u8;
        right = "aabbccdde1f"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(9, actual);

        left = "aabbccddeef"u8;
        right = "aabbccdd1ef"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(8, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector256Loop_2VectorsExact()
    {
        var left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        var right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(64, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhh1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(63, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffff1ggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(48, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeefffffff1gggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(47, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccdddddddd1eeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(32, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1eeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(31, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbcccccccc1dddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(24, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbb1cccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(16, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbb1ccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(15, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "1aaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector256Loop_2VectorsOverlap()
    {
        var left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        var right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(56, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffggggggg1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(55, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeee1fffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(40, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeee1ffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(39, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbcccccccc1dddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(24, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1eeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(31, actual);

        left = "aaaaaaaabbbbbbbb1cccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(16, actual);

        left = "aaaaaaaabbbbbbb1ccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(15, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "1aaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        right = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(33, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        right = "aaaaaaaabbbbbbbbccccccccdddddddd1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(32, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1e"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(31, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector256Loop_1VectorExact()
    {
        var left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        var right = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(32, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(31, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "aaaaaaaabbbbbbbb1cccccccdddddddd"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(16, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "aaaaaaaabbbbbbb1ccccccccdddddddd"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(15, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "1aaaaaaabbbbbbbbccccccccdddddddd"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector128Loop_2VectorsOverlap()
    {
        var left = "aaaaaaaabbbbbbbbcccccccc"u8;
        var right = "aaaaaaaabbbbbbbbcccccccc"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(24, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaabbbbbbbbccccccc1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(23, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaabbbbbbbb1ccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(16, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaabbbbbbb1cccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(15, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaa1bbbbbbbcccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(8, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaa1bbbbbbbbcccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(7, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "1aaaaaaabbbbbbbbcccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);

        left = "aaaaaaaabbbbbbbbc"u8;
        right = "aaaaaaaabbbbbbbbc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(17, actual);

        left = "aaaaaaaabbbbbbbbc"u8;
        right = "aaaaaaaabbbbbbbb1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(16, actual);

        left = "aaaaaaaabbbbbbbbc"u8;
        right = "a1aaaaaabbbbbbbbc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(1, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector128Loop_1VectorExact()
    {
        var left = "aaaaaaaabbbbbbbb"u8;
        var right = "aaaaaaaabbbbbbbb"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(16, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "aaaaaaaabbbbbbb1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(15, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "aaaaaaaa1bbbbbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(8, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "aaaaaaa1bbbbbbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(7, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "1aaaaaaabbbbbbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), Math.Min(left.Length, right.Length));
        Assert.AreEqual(0, actual);
    }
}