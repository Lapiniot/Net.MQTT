using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.MqttTopicHelpersTests
{
    [TestClass]
    public class MqttTopicHelpers_IsValidTopic_Should
    {
        [TestMethod]
        public void ReturnFalse_GivenNullTopic()
        {
            var actual = IsValidTopic(null);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptyTopic()
        {
            var actual = IsValidTopic(string.Empty);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcardOnly()
        {
            var actual = IsValidTopic("#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenSingleLevelWildcardOnly()
        {
            var actual = IsValidTopic("+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenLevelSeparatorOnly()
        {
            var actual = IsValidTopic("/");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcardAtLastLevel()
        {
            var actual = IsValidTopic("a/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenMultiLevelWildcardAtNotLastLevel()
        {
            var actual = IsValidTopic("a/#/b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenMultiLevelWildcardAsPartOfLevel()
        {
            var actual = IsValidTopic("a/b#");
            Assert.IsFalse(actual);

            actual = IsValidTopic("a/#b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenSingleLevelWildcardAtAnyLevel()
        {
            var actual = IsValidTopic("+/a/b");
            Assert.IsTrue(actual);

            actual = IsValidTopic("a/+/b");
            Assert.IsTrue(actual);

            actual = IsValidTopic("a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultipleSingleLevelWildcards()
        {
            var actual = IsValidTopic("+/a/+");
            Assert.IsTrue(actual);

            actual = IsValidTopic("+/+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenSingleLevelWildcardAsPartOfLevel()
        {
            var actual = IsValidTopic("a/b+");
            Assert.IsFalse(actual);

            actual = IsValidTopic("a/b+/");
            Assert.IsFalse(actual);

            actual = IsValidTopic("a/+b");
            Assert.IsFalse(actual);

            actual = IsValidTopic("a/+b/");
            Assert.IsFalse(actual);
        }
    }
}