using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.MqttExtensions;

namespace System.Net.Mqtt.Tests.MqttExtensions;

[TestClass]
public class TopicMatchesShould
{
    [TestMethod]
    public void ReturnFalseGivenNullTopic()
    {
        var actual = TopicMatches(null, "");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenEmptyTopic()
    {
        var actual = TopicMatches(string.Empty, "");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildCardOnly()
    {
        var actual = TopicMatches("a/b/c", "#");
        Assert.IsTrue(actual);

        actual = TopicMatches("a", "#");
        Assert.IsTrue(actual);

        actual = TopicMatches("/", "#");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildCard()
    {
        var actual = TopicMatches("a/b/c", "a/b/#");
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c", "a/#");
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/c/", "a/b/c/#");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenStrictMatch()
    {
        var actual = TopicMatches("a/b/c", "a/b/c");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenStrictFilterAndPartialMatch()
    {
        var actual = TopicMatches("a/b/c/d", "a/b/c");
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/c", "a/b/c/d");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenStrictFilterNotMatchingTopic()
    {
        var actual = TopicMatches("c/d/e", "a/b/c");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcard()
    {
        var actual = TopicMatches("aaaa/b/c", "+/b/c");
        Assert.IsTrue(actual);

        actual = TopicMatches("a/bbbb/c", "a/+/c");
        Assert.IsTrue(actual);

        actual = TopicMatches("a/b/cccc", "a/b/+");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndMoreLevelsTopic()
    {
        var actual = TopicMatches("aa/aa/b/c", "+/b/c");
        Assert.IsFalse(actual);

        actual = TopicMatches("a/bb/bb/c", "a/+/c");
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/cc/cc", "a/b/+");
        Assert.IsFalse(actual);

        actual = TopicMatches("a/b/cccc/", "a/b/+");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcardAndTrailingSlash()
    {
        var actual = TopicMatches("a/b/", "a/b/+");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenOneLevelWildcardAndLeadingSlash()
    {
        var actual = TopicMatches("/b/c", "+/b/c");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenTwoOneLevelWildcardsAndSlashTopic()
    {
        var actual = TopicMatches("/", "+/+");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndSlashTopic()
    {
        var actual = TopicMatches("/", "+");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildcardAndSlashTopic()
    {
        var actual = TopicMatches("/", "#");
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndExtraTrailingSlash()
    {
        var actual = TopicMatches("a/b/cccc/", "a/b/+");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenOneLevelWildcardAndExtraLeadingSlash()
    {
        var actual = TopicMatches("/aaaa/b/c", "+/b/c");
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultipleOneLevelWildcardFilter()
    {
        var actual = TopicMatches("a/bbbb/c/dddd/e", "a/+/c/+/e");
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaa/b/cccc/d/eeee/f/gggg", "+/b/+/d/+/f/+");
        Assert.IsTrue(actual);

        actual = TopicMatches("aaaa/bbbb/cccc", "+/+/+");
        Assert.IsTrue(actual);
    }
}