using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.MqttExtensions;

namespace System.Net.Mqtt.Tests.MqttExtensions;

[TestClass]
public class TopicMatchesShould
{
    [TestMethod]
    public void ReturnTrueGivenEmptyTopicAndEmptyFilter()
    {
        var actual = TopicMatches(Utf8String.Empty.Span, Utf8String.Empty.Span);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenEmptyTopicAndNotEmptyFilter()
    {
        var actual = TopicMatches(Utf8String.Empty.Span, UTF8.GetBytes("a/b/c/d"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenNotEmptyTopicAndEmptyFilter()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/b/c/d"), Utf8String.Empty.Span);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildCardOnly()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/b/c"), UTF8.GetBytes("#"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("a"), UTF8.GetBytes("#"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("/"), UTF8.GetBytes("#"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildCard()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/b/c"), UTF8.GetBytes("a/b/#"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("a/b/c"), UTF8.GetBytes("a/#"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("a/b/c/"), UTF8.GetBytes("a/b/c/#"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenStrictMatch()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/b/c"), UTF8.GetBytes("a/b/c"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenStrictFilterAndPartialMatch()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/b/c/d"), UTF8.GetBytes("a/b/c"));
        Assert.IsFalse(actual);

        actual = TopicMatches(UTF8.GetBytes("a/b/c"), UTF8.GetBytes("a/b/c/d"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenStrictFilterNotMatchingTopic()
    {
        var actual = TopicMatches(UTF8.GetBytes("c/d/e"), UTF8.GetBytes("a/b/c"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcard()
    {
        var actual = TopicMatches(UTF8.GetBytes("aaaa/b/c"), UTF8.GetBytes("+/b/c"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("a/bbbb/c"), UTF8.GetBytes("a/+/c"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("a/b/cccc"), UTF8.GetBytes("a/b/+"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndMoreLevelsTopic()
    {
        var actual = TopicMatches(UTF8.GetBytes("aa/aa/b/c"), UTF8.GetBytes("+/b/c"));
        Assert.IsFalse(actual);

        actual = TopicMatches(UTF8.GetBytes("a/bb/bb/c"), UTF8.GetBytes("a/+/c"));
        Assert.IsFalse(actual);

        actual = TopicMatches(UTF8.GetBytes("a/b/cc/cc"), UTF8.GetBytes("a/b/+"));
        Assert.IsFalse(actual);

        actual = TopicMatches(UTF8.GetBytes("a/b/cccc/"), UTF8.GetBytes("a/b/+"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcardAndTrailingSlash()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/b/"), UTF8.GetBytes("a/b/+"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcardAndLeadingSlash()
    {
        var actual = TopicMatches(UTF8.GetBytes("/b/c"), UTF8.GetBytes("+/b/c"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenTwoOneLevelWildcardsAndSlashTopic()
    {
        var actual = TopicMatches(UTF8.GetBytes("/"), UTF8.GetBytes("+/+"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndSlashTopic()
    {
        var actual = TopicMatches(UTF8.GetBytes("/"), UTF8.GetBytes("+"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildcardAndSlashTopic()
    {
        var actual = TopicMatches(UTF8.GetBytes("/"), UTF8.GetBytes("#"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndExtraTrailingSlash()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/b/cccc/"), UTF8.GetBytes("a/b/+"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndExtraLeadingSlash()
    {
        var actual = TopicMatches(UTF8.GetBytes("/aaaa/b/c"), UTF8.GetBytes("+/b/c"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultipleOneLevelWildcardFilter()
    {
        var actual = TopicMatches(UTF8.GetBytes("a/bbbb/c/dddd/e"), UTF8.GetBytes("a/+/c/+/e"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("aaaa/b/cccc/d/eeee/f/gggg"), UTF8.GetBytes("+/b/+/d/+/f/+"));
        Assert.IsTrue(actual);

        actual = TopicMatches(UTF8.GetBytes("aaaa/bbbb/cccc"), UTF8.GetBytes("+/+/+"));
        Assert.IsTrue(actual);
    }
}