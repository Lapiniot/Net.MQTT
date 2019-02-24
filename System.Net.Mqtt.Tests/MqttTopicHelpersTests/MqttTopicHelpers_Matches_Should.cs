using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.MqttTopicHelpersTests
{
    [TestClass]
    public class MqttTopicHelpers_Matches_Should
    {
        [TestMethod]
        public void ReturnFalse_GivenNullTopic()
        {
            var actual = MqttExtensions.TopicMatches(null, "");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptyTopic()
        {
            var actual = MqttExtensions.TopicMatches(string.Empty, "");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildCardOnly()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c", "#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a", "#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("/", "#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildCard()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c", "a/b/#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/b/c", "a/#");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/b/c/", "a/b/c/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenStrictMatch()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c", "a/b/c");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenStrictFilter_AndPartialMatch()
        {
            var actual = MqttExtensions.TopicMatches("a/b/c/d", "a/b/c");
            Assert.IsFalse(actual);

            actual = MqttExtensions.TopicMatches("a/b/c", "a/b/c/d");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenStrictFilter_NotMatchingTopic()
        {
            var actual = MqttExtensions.TopicMatches("c/d/e", "a/b/c");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenOneLevelWildcard()
        {
            var actual = MqttExtensions.TopicMatches("aaaa/b/c", "+/b/c");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/bbbb/c", "a/+/c");
            Assert.IsTrue(actual);

            actual = MqttExtensions.TopicMatches("a/b/cccc", "a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndMoreLevelsTopic()
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
        public void ReturnTrue_GivenOneLevelWildcard_AndTrailingSlash()
        {
            var actual = MqttExtensions.TopicMatches("a/b/", "a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenOneLevelWildcard_AndLeadingSlash()
        {
            var actual = MqttExtensions.TopicMatches("/b/c", "+/b/c");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenTwoOneLevelWildcards_AndSlashTopic()
        {
            var actual = MqttExtensions.TopicMatches("/", "+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndSlashTopic()
        {
            var actual = MqttExtensions.TopicMatches("/", "+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcard_AndSlashTopic()
        {
            var actual = MqttExtensions.TopicMatches("/", "#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndExtraTrailingSlash()
        {
            var actual = MqttExtensions.TopicMatches("a/b/cccc/", "a/b/+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndExtraLeadingSlash()
        {
            var actual = MqttExtensions.TopicMatches("/aaaa/b/c", "+/b/c");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultipleOneLevelWildcardFilter()
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