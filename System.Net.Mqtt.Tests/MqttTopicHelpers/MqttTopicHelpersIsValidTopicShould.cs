using System.Net.Mqtt.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.Tests.MqttTopicHelpersTests
{
    [TestClass]
    public class MqttTopicHelpersIsValidTopicShould
    {
        [TestMethod]
        public void ReturnFalseGivenNullTopic()
        {
            var actual = MqttExtensions.IsValidTopic(null);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenEmptyTopic()
        {
            var actual = MqttExtensions.IsValidTopic(string.Empty);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultiLevelWildcardOnly()
        {
            var actual = MqttExtensions.IsValidTopic("#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenSingleLevelWildcardOnly()
        {
            var actual = MqttExtensions.IsValidTopic("+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenLevelSeparatorOnly()
        {
            var actual = MqttExtensions.IsValidTopic("/");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultiLevelWildcardAtLastLevel()
        {
            var actual = MqttExtensions.IsValidTopic("a/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenMultiLevelWildcardAtNotLastLevel()
        {
            var actual = MqttExtensions.IsValidTopic("a/#/b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenMultiLevelWildcardAsPartOfLevel()
        {
            var actual = MqttExtensions.IsValidTopic("a/b#");
            Assert.IsFalse(actual);

            actual = MqttExtensions.IsValidTopic("a/#b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenSingleLevelWildcardAtAnyLevel()
        {
            var actual = MqttExtensions.IsValidTopic("+/a/b");
            Assert.IsTrue(actual);

            actual = MqttExtensions.IsValidTopic("a/+/b");
            Assert.IsTrue(actual);

            actual = MqttExtensions.IsValidTopic("a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultipleSingleLevelWildcards()
        {
            var actual = MqttExtensions.IsValidTopic("+/a/+");
            Assert.IsTrue(actual);

            actual = MqttExtensions.IsValidTopic("+/+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenSingleLevelWildcardAsPartOfLevel()
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