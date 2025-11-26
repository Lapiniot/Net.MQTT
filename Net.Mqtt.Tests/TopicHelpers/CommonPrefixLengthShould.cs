using System.Runtime.CompilerServices;
using static Net.Mqtt.TopicHelpers;

namespace Net.Mqtt.Tests.TopicHelpers;

[TestClass]
public class CommonPrefixLengthShould
{
    [TestMethod]
    public void ReturnZeroGivenZeroLength()
    {
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in "abc"u8[0]), ref Unsafe.AsRef(in "def"u8[0]), 0);
        Assert.AreEqual(0u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsUnrolledLoop()
    {
        var left = "aaaabbbbcccc"u8;
        var right = "aaaabbbbcccc"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(12u, actual);

        left = "aaaabbbbccc1"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(11u, actual);

        left = "aaaabbbbcc1c"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(10u, actual);

        left = "aaaabbbbc1cc"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(9u, actual);

        left = "aaaabbbb1ccc"u8;
        right = "aaaabbbbcccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(8u, actual);

        left = "1aaabbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);

        left = "a1aabbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(1u, actual);

        left = "aa1abbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(2u, actual);

        left = "aaa1bbbb"u8;
        right = "aaaabbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(3u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsSimpleLoop()
    {
        var left = "abc"u8;
        var right = "abc"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(3u, actual);

        left = "abc"u8;
        right = "ab1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(2u, actual);

        left = "abc"u8;
        right = "a1c"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(1u, actual);

        left = "abc"u8;
        right = "1bc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);

        left = "a"u8;
        right = "b"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsBothLoops()
    {
        var left = "aabbccddeef"u8;
        var right = "aabbccddeef"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(11u, actual);

        left = "aabbccddeef"u8;
        right = "aabbccddee1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(10u, actual);

        left = "aabbccddeef"u8;
        right = "aabbccdde1f"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(9u, actual);

        left = "aabbccddeef"u8;
        right = "aabbccdd1ef"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(8u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector256Loop_2VectorsExact()
    {
        var left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        var right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(64u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhh1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(63u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffff1ggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(48u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeefffffff1gggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(47u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccdddddddd1eeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(32u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1eeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(31u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbbcccccccc1dddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(24u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbbb1cccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(16u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "aaaaaaaabbbbbbb1ccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(15u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        right = "1aaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggghhhhhhhh"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector256Loop_2VectorsOverlap()
    {
        var left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        var right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(56u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffggggggg1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(55u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeee1fffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(40u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeee1ffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(39u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbcccccccc1dddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(24u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1eeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(31u, actual);

        left = "aaaaaaaabbbbbbbb1cccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(16u, actual);

        left = "aaaaaaaabbbbbbb1ccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(15u, actual);

        left = "aaaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        right = "1aaaaaaabbbbbbbbccccccccddddddddeeeeeeeeffffffffgggggggg"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        right = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(33u, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        right = "aaaaaaaabbbbbbbbccccccccdddddddd1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(32u, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddde"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1e"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(31u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector256Loop_1VectorExact()
    {
        var left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        var right = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(32u, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "aaaaaaaabbbbbbbbccccccccddddddd1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(31u, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "aaaaaaaabbbbbbbb1cccccccdddddddd"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(16u, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "aaaaaaaabbbbbbb1ccccccccdddddddd"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(15u, actual);

        left = "aaaaaaaabbbbbbbbccccccccdddddddd"u8;
        right = "1aaaaaaabbbbbbbbccccccccdddddddd"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector128Loop_2VectorsOverlap()
    {
        var left = "aaaaaaaabbbbbbbbcccccccc"u8;
        var right = "aaaaaaaabbbbbbbbcccccccc"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(24u, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaabbbbbbbbccccccc1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(23u, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaabbbbbbbb1ccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(16u, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaabbbbbbb1cccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(15u, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaaa1bbbbbbbcccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(8u, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "aaaaaaa1bbbbbbbbcccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(7u, actual);

        left = "aaaaaaaabbbbbbbbcccccccc"u8;
        right = "1aaaaaaabbbbbbbbcccccccc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);

        left = "aaaaaaaabbbbbbbbc"u8;
        right = "aaaaaaaabbbbbbbbc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(17u, actual);

        left = "aaaaaaaabbbbbbbbc"u8;
        right = "aaaaaaaabbbbbbbb1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(16u, actual);

        left = "aaaaaaaabbbbbbbbc"u8;
        right = "a1aaaaaabbbbbbbbc"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(1u, actual);
    }

    [TestMethod]
    public void ReturnPrefixLength_HitsVector128Loop_1VectorExact()
    {
        var left = "aaaaaaaabbbbbbbb"u8;
        var right = "aaaaaaaabbbbbbbb"u8;
        var actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(16u, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "aaaaaaaabbbbbbb1"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(15u, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "aaaaaaaa1bbbbbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(8u, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "aaaaaaa1bbbbbbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(7u, actual);

        left = "aaaaaaaabbbbbbbb"u8;
        right = "1aaaaaaabbbbbbbb"u8;
        actual = CommonPrefixLength(ref Unsafe.AsRef(in left[0]), ref Unsafe.AsRef(in right[0]), (nuint)Math.Min(left.Length, right.Length));
        Assert.AreEqual(0u, actual);
    }
}