using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.MqttTopicHelpersTests
{
    [TestClass]
    public class MqttTopicHelpersMatchesShould
    {
        [TestMethod]
        public void ReturnFalseGivenNullTopic()
        {
            var actual = MqttExtensions.TopicMatches(null, "");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenEmptyTopic()
        {
            var actual = MqttExtensions.TopicMatches(string.Empty, "");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultiLevelWildCardOnly()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c", "#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a", "#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("/", "#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultiLevelWildCard()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c", "a/b/#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/b/c", "a/#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/b/c/", "a/b/c/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenStrictMatch()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c", "a/b/c");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenStrictFilterAndPartialMatch()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c/d", "a/b/c");
            Assert.IsFalse(actual);

            actual = MqttExtensions.TopicMatches("a/b/c", "a/b/c/d");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenStrictFilterNotMatchingTopic()
        {
            var actual = MqttExtensions.TopicMatches("c/d/e", "a/b/c");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenOneLevelWildcard()
        {
            var actual = MqttExtensions.TopicMatches("aaaa/b/c", "+/b/c");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/bbbb/c", "a/+/c");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/b/cccc", "a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenOneLevelWildcardAndMoreLevelsTopic()
        {
            var actual = MqttExtensions.TopicMatches("aa/aa/b/c", "+/b/c");
            Assert.IsFalse(actual);

            actual = MqttExtensions.TopicMatches("a/bb/bb/c", "a/+/c");
            Assert.IsFalse(actual);

            actual = MqttExtensions.TopicMatches("a/b/cc/cc", "a/b/+");
            Assert.IsFalse(actual);

            actual = MqttExtensions.TopicMatches("a/b/cccc/", "a/b/+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenOneLevelWildcardAndTrailingSlash()
        {
            var actual = MqttExtensions.TopicMatches("a/b/", "a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenOneLevelWildcardAndLeadingSlash()
        {
            var actual = MqttExtensions.TopicMatches("/b/c", "+/b/c");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenTwoOneLevelWildcardsAndSlashTopic()
        {
            var actual = MqttExtensions.TopicMatches("/", "+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenOneLevelWildcardAndSlashTopic()
        {
            var actual = MqttExtensions.TopicMatches("/", "+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultiLevelWildcardAndSlashTopic()
        {
            var actual = MqttExtensions.TopicMatches("/", "#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenOneLevelWildcardAndExtraTrailingSlash()
        {
            var actual = MqttExtensions.TopicMatches("a/b/cccc/", "a/b/+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenOneLevelWildcardAndExtraLeadingSlash()
        {
            var actual = MqttExtensions.TopicMatches("/aaaa/b/c", "+/b/c");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultipleOneLevelWildcardFilter()
        {
            var actual = MqttExtensions.TopicMatches("a/bbbb/c/dddd/e", "a/+/c/+/e");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("aaaa/b/cccc/d/eeee/f/gggg", "+/b/+/d/+/f/+");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("aaaa/bbbb/cccc", "+/+/+");
            Assert.IsTrue(actual);
        }
    }
}