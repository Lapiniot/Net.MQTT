using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.MqttExtensions;

namespace System.Net.Mqtt.Tests.MqttExtensions;

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
        var actual = IsValidFilter(Utf8String.Empty.Span);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildcardOnly()
    {
        var actual = IsValidFilter(UTF8.GetBytes("#"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenSingleLevelWildcardOnly()
    {
        var actual = IsValidFilter(UTF8.GetBytes("+"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenLevelSeparatorOnly()
    {
        var actual = IsValidFilter(UTF8.GetBytes("/"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultiLevelWildcardAtLastLevel()
    {
        var actual = IsValidFilter(UTF8.GetBytes("a/#"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenMultiLevelWildcardAtNotLastLevel()
    {
        var actual = IsValidFilter(UTF8.GetBytes("a/#/b"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenMultiLevelWildcardAsPartOfLevel()
    {
        var actual = IsValidFilter(UTF8.GetBytes("a/b#"));
        Assert.IsFalse(actual);

        actual = IsValidFilter(UTF8.GetBytes("a/#b"));
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenSingleLevelWildcardAtAnyLevel()
    {
        var actual = IsValidFilter(UTF8.GetBytes("+/a/b"));
        Assert.IsTrue(actual);

        actual = IsValidFilter(UTF8.GetBytes("a/+/b"));
        Assert.IsTrue(actual);

        actual = IsValidFilter(UTF8.GetBytes("a/b/+"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnTrueGivenMultipleSingleLevelWildcards()
    {
        var actual = IsValidFilter(UTF8.GetBytes("+/a/+"));
        Assert.IsTrue(actual);

        actual = IsValidFilter(UTF8.GetBytes("+/+/+"));
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void ReturnFalseGivenSingleLevelWildcardAsPartOfLevel()
    {
        var actual = IsValidFilter(UTF8.GetBytes("a/b+"));
        Assert.IsFalse(actual);

        actual = IsValidFilter(UTF8.GetBytes("a/b+/"));
        Assert.IsFalse(actual);

        actual = IsValidFilter(UTF8.GetBytes("a/+b"));
        Assert.IsFalse(actual);

        actual = IsValidFilter(UTF8.GetBytes("a/+b/"));
        Assert.IsFalse(actual);
    }
}