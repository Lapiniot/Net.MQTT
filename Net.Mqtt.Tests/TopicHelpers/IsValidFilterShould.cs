using static Net.Mqtt.TopicHelpers;

namespace Net.Mqtt.Tests.TopicHelpers;

[TestClass]
public class IsValidFilterShould
{
    [TestMethod]
    public void ReturnFalseGivenNullTopic()
    {
        var actual = IsValidFilter(null);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenEmptyTopic()
    {
        var actual = IsValidFilter([]);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildcardOnly()
    {
        var actual = IsValidFilter("#"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenSingleLevelWildcardOnly()
    {
        var actual = IsValidFilter("+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenLevelSeparatorOnly()
    {
        var actual = IsValidFilter("/"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildcardAtLastLevel()
    {
        var actual = IsValidFilter("a/#"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenMultiLevelWildcardAtNotLastLevel()
    {
        var actual = IsValidFilter("a/#/b"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenMultiLevelWildcardAsPartOfLevel()
    {
        var actual = IsValidFilter("a/b#"u8);
        Assert.IsFalse(actual);

        actual = IsValidFilter("a/#b"u8);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenSingleLevelWildcardAtAnyLevel()
    {
        var actual = IsValidFilter("+/a/b"u8);
        Assert.IsTrue(actual);

        actual = IsValidFilter("a/+/b"u8);
        Assert.IsTrue(actual);

        actual = IsValidFilter("a/b/+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultipleSingleLevelWildcards()
    {
        var actual = IsValidFilter("+/a/+"u8);
        Assert.IsTrue(actual);

        actual = IsValidFilter("+/+/+"u8);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenSingleLevelWildcardAsPartOfLevel()
    {
        var actual = IsValidFilter("a/b+"u8);
        Assert.IsFalse(actual);

        actual = IsValidFilter("a/b+/"u8);
        Assert.IsFalse(actual);

        actual = IsValidFilter("a/+b"u8);
        Assert.IsFalse(actual);

        actual = IsValidFilter("a/+b/"u8);
        Assert.IsFalse(actual);
    }
}