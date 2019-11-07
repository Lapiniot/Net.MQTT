using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.MqttTopicHelpersTests
{
    [TestClass]
    public class MqttTopicHelpers_IsValidTopic_Should
    {
        [TestMethod]
        public void ReturnFalse_GivenNullTopic()
        {
            var actual = MqttExtensions.IsValidTopic(null);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptyTopic()
        {
            var actual = MqttExtensions.IsValidTopic(string.Empty);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcardOnly()
        {
            var actual = MqttExtensions.IsValidTopic("#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenSingleLevelWildcardOnly()
        {
            var actual = MqttExtensions.IsValidTopic("+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenLevelSeparatorOnly()
        {
            var actual = MqttExtensions.IsValidTopic("/");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcardAtLastLevel()
        {
            var actual = MqttExtensions.IsValidTopic("a/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenMultiLevelWildcardAtNotLastLevel()
        {
            var actual = MqttExtensions.IsValidTopic("a/#/b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenMultiLevelWildcardAsPartOfLevel()
        {
            var actual = MqttExtensions.IsValidTopic("a/b#");
            Assert.IsFalse(actual);

            actual = MqttExtensions.IsValidTopic("a/#b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenSingleLevelWildcardAtAnyLevel()
        {
            var actual = MqttExtensions.IsValidTopic("+/a/b");
            Assert.IsTrue(actual);

            actual = MqttExtensions.IsValidTopic("a/+/b");
            Assert.IsTrue(actual);

            actual = MqttExtensions.IsValidTopic("a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultipleSingleLevelWildcards()
        {
            var actual = MqttExtensions.IsValidTopic("+/a/+");
            Assert.IsTrue(actual);

            actual = MqttExtensions.IsValidTopic("+/+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenSingleLevelWildcardAsPartOfLevel()
        {
            var actual = MqttExtensions.IsValidTopic("a/b+");
            Assert.IsFalse(actual);

            actual = MqttExtensions.IsValidTopic("a/b+/");
            Assert.IsFalse(actual);

            actual = MqttExtensions.IsValidTopic("a/+b");
            Assert.IsFalse(actual);

            actual = MqttExtensions.IsValidTopic("a/+b/");
            Assert.IsFalse(actual);
        }
    }
}