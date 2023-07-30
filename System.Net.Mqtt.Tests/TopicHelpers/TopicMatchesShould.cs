using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.TopicHelpers;

namespace System.Net.Mqtt.Tests.TopicHelpers;

[TestClass]
public class TopicMatchesShould
{
    [TestMethod]
    public void ReturnFalseGivenEmptyTopicAndEmptyFilter()
    {
        var actual = TopicMatches(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenEmptyTopicAndNotEmptyFilter()
    {
        var actual = TopicMatches(ReadOnlySpan<byte>.Empty, "a/b/c/d"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenNotEmptyTopicAndEmptyFilter()
    {
        var actual = TopicMatches("a/b/c/d"u8, ReadOnlySpan<byte>.Empty);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildCardOnly()
    {
        var actual = TopicMatches("a/b/c"u8, "#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a"u8, "#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("/"u8, "#"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWithWildCard()
    {
        var actual = TopicMatches("a/b/c/d/e/f"u8, "a/b/c/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c/d/e/"u8, "a/b/c/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c/"u8, "a/b/c/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c"u8, "a/b/c/#"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenMultiLevelWithWildCardPartialMatch()
    {
        var actual = TopicMatches("aaabbb"u8, "aaa/#"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWithWildCardPartialMatch()
    {
        var actual = TopicMatches("aaabbb"u8, "aaa/+"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenStrictMatch()
    {
        var actual = TopicMatches("aaaaaaaa/bbbbbbbb/cccccccc/dddddddd/12"u8, "aaaaaaaa/bbbbbbbb/cccccccc/dddddddd/12"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaa/bbbb/cccc/12"u8, "aaaa/bbbb/cccc/12"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaa/bbbb/cccc"u8, "aaaa/bbbb/cccc"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aa/bb/cc"u8, "aa/bb/cc"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c"u8, "a/b/c"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("abc"u8, "abc"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenStrictFilterAndPartialMatch()
    {
        var actual = TopicMatches("aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc"u8, "aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc/ddddddddddddddd"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc"u8, "aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc/"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc/dddddddddddddddddd"u8, "aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc/"u8, "aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaa/bbbb/cccc/12"u8, "aaaa/bbbb/cccc/13"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaa/bbbb/cccc"u8, "aaaa/bbbb/cccc/dddd"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaa/bbbb/cccc"u8, "aaaa/bbbb/cccc/"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaa/bbbb/cccc/dddd"u8, "aaaa/bbbb/cccc"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aaaa/bbbb/cccc/"u8, "aaaa/bbbb/cccc"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aa/bb/cc"u8, "aa/bb/cc/dd"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aa/bb/cc"u8, "aa/bb/cc/"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aa/bb/cc/dd"u8, "aa/bb/cc"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("aa/bb/cc/"u8, "aa/bb/cc"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenStrictFilterNotMatchingTopic()
    {
        var actual = TopicMatches("fffffffffff/bbbbbbbbbbbbbb/cccccccccccccccc"u8, "aaaaaaaaaaaa/bbbbbbbbbbbbbb/cccccccccccccccc/ddddddddddddddd"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("fffffffffff/11111/bbbbbbbbbbbbbb/cccccccccccccccc"u8, "fffffffffff/22222/bbbbbbbbbbbbbb/cccccccccccccccc"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("fffffffffff/bbbbbbbbbbbbbb/cccccccccccccccc/11"u8, "fffffffffff/bbbbbbbbbbbbbb/cccccccccccccccc/22"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("c/d/e"u8, "a/b/c"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcard()
    {
        var actual = TopicMatches("aaaa/b/c"u8, "+/b/c"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/bbbb/c"u8, "a/+/c"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/cccc"u8, "a/b/+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcardWithEmptyLevel()
    {
        var actual = TopicMatches("/b/c"u8, "+/b/c"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a//c"u8, "a/+/c"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/"u8, "a/b/+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndMoreLevelsTopic()
    {
        var actual = TopicMatches("aa/aa/b/c"u8, "+/b/c"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("/aa/b/c"u8, "+/b/c"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("a/bb/bb/c"u8, "a/+/c"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/cc/cc"u8, "a/b/+"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/cccc/"u8, "a/b/+"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("/a/b/cccc"u8, "a/b/+"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/cccc"u8, "a/b/+/"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/cccc"u8, "a/b/+/d"u8);
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/cccc"u8, "a/b/+/+"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcardAndOneLevelTopic()
    {
        var actual = TopicMatches("aaaa"u8, "+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenTwoOneLevelWildcardsAndSlashTopic()
    {
        var actual = TopicMatches("/"u8, "+/+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndSlashTopic()
    {
        var actual = TopicMatches("/"u8, "+"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildcardAndSlashTopic()
    {
        var actual = TopicMatches("/"u8, "#"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultipleOneLevelWildcardFilter()
    {
        var actual = TopicMatches("a/bbbb/c/dddd/e"u8, "a/+/c/+/e"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/bbbbbbbbbbbbbbbb/c/dddddddddddddddd/e"u8, "a/+/c/+/e"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/bbbbbbbbbbbb/c/dddddddddddddddd/e"u8, "a/+/c/+/e"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaa/b/cccc/d/eeee/f/gggg"u8, "+/b/+/d/+/f/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaaaaaaaaaaaaaa/b/cccccccccccccccc/d/eeeeeeeeeeeeeeee/f/gggggggggggggggg"u8, "+/b/+/d/+/f/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaaaaaaaaaa/b/cccccccccccc/d/eeeeeeeeeeee/f/gggggggggggg"u8, "+/b/+/d/+/f/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaa/bbbb/cccc"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaaaaaaaaaaaaaa/bbbbbbbbbbbbbbbb/cccccccccccccccc"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaaaaaaaaaa/bbbbbbbbbbbb/cccccccccccc"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("/bbbb/cccc"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("/bbbbbbbbbbbbbbbb/cccccccccccccccc"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("/bbbbbbbbbbbb/cccccccccccc"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaa/bbbb/"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaaaaaaaaaaaaaa/bbbbbbbbbbbbbbbb/"u8, "+/+/+"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaaaaaaaaaa/bbbbbbbbbbbb/"u8, "+/+/+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenComplexWildcardsFilter()
    {
        var actual = TopicMatches("a/bbbb/c/dddd/e"u8, "a/+/c/+/e/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/bbbb/c/dddd/e/"u8, "a/+/c/+/e/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/bbbb/c/dddd/e/"u8, "+/+/+/+/+/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c/d/e"u8, "a/b/c/d/+/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c/d/"u8, "a/b/c/d/+/#"u8);
        Assert.IsTrue(actual);

        actual = TopicMatches("a/bbbb/c/dddd/e/"u8, "+/+/+/#"u8);
        Assert.IsTrue(actual);
    }
}