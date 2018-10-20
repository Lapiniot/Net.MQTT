using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.MqttTopicHelpersTests
{
    [TestClass]
    public class MqttTopicHelpers_Matches_Should
    {
        [TestMethod]
        public void ReturnFalse_GivenNullTopic()
        {
            var actual = Matches(null, "");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptyTopic()
        {
            var actual = Matches(string.Empty, "");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildCardOnly()
        {
            var actual = Matches("a/b/c", "#");
            Assert.IsTrue(actual);

            actual = Matches("a", "#");
            Assert.IsTrue(actual);

            actual = Matches("/", "#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildCard()
        {
            var actual = Matches("a/b/c", "a/b/#");
            Assert.IsTrue(actual);

            actual = Matches("a/b/c", "a/#");
            Assert.IsTrue(actual);

            actual = Matches("a/b/c/", "a/b/c/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenStrictMatch()
        {
            var actual = Matches("a/b/c", "a/b/c");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenStrictFilter_AndPartialMatch()
        {
            var actual = Matches("a/b/c/d", "a/b/c");
            Assert.IsFalse(actual);

            actual = Matches("a/b/c", "a/b/c/d");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenStrictFilter_NotMatchingTopic()
        {
            var actual = Matches("c/d/e", "a/b/c");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenOneLevelWildcard()
        {
            var actual = Matches("aaaa/b/c", "+/b/c");
            Assert.IsTrue(actual);

            actual = Matches("a/bbbb/c", "a/+/c");
            Assert.IsTrue(actual);

            actual = Matches("a/b/cccc", "a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndMoreLevelsTopic()
        {
            var actual = Matches("aa/aa/b/c", "+/b/c");
            Assert.IsFalse(actual);

            actual = Matches("a/bb/bb/c", "a/+/c");
            Assert.IsFalse(actual);

            actual = Matches("a/b/cc/cc", "a/b/+");
            Assert.IsFalse(actual);

            actual = Matches("a/b/cccc/", "a/b/+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenOneLevelWildcard_AndTrailingSlash()
        {
            var actual = Matches("a/b/", "a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenOneLevelWildcard_AndLeadingSlash()
        {
            var actual = Matches("/b/c", "+/b/c");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenTwoOneLevelWildcards_AndSlashTopic()
        {
            var actual = Matches("/", "+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndSlashTopic()
        {
            var actual = Matches("/", "+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcard_AndSlashTopic()
        {
            var actual = Matches("/", "#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndExtraTrailingSlash()
        {
            var actual = Matches("a/b/cccc/", "a/b/+");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenOneLevelWildcard_AndExtraLeadingSlash()
        {
            var actual = Matches("/aaaa/b/c", "+/b/c");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultipleOneLevelWildcardFilter()
        {
            var actual = Matches("a/bbbb/c/dddd/e", "a/+/c/+/e");
            Assert.IsTrue(actual);

            actual = Matches("aaaa/b/cccc/d/eeee/f/gggg", "+/b/+/d/+/f/+");
            Assert.IsTrue(actual);

            actual = Matches("aaaa/bbbb/cccc", "+/+/+");
            Assert.IsTrue(actual);
        }
    }
}