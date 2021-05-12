using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mqtt.Extensions.MqttExtensions;

namespace System.Net.Mqtt.Tests.MqttExtensions
{
    [TestClass]
    public class IsValidTopicShould
    {
        [TestMethod]
        public void ReturnFalseGivenNullTopic()
        {
            var actual = IsValidTopic(null);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenEmptyTopic()
        {
            var actual = IsValidTopic(string.Empty);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultiLevelWildcardOnly()
        {
            var actual = IsValidTopic("#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenSingleLevelWildcardOnly()
        {
            var actual = IsValidTopic("+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenLevelSeparatorOnly()
        {
            var actual = IsValidTopic("/");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultiLevelWildcardAtLastLevel()
        {
            var actual = IsValidTopic("a/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenMultiLevelWildcardAtNotLastLevel()
        {
            var actual = IsValidTopic("a/#/b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenMultiLevelWildcardAsPartOfLevel()
        {
            var actual = IsValidTopic("a/b#");
            Assert.IsFalse(actual);

            actual = IsValidTopic("a/#b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenSingleLevelWildcardAtAnyLevel()
        {
            var actual = IsValidTopic("+/a/b");
            Assert.IsTrue(actual);

            actual = IsValidTopic("a/+/b");
            Assert.IsTrue(actual);

            actual = IsValidTopic("a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrueGivenMultipleSingleLevelWildcards()
        {
            var actual = IsValidTopic("+/a/+");
            Assert.IsTrue(actual);

            actual = IsValidTopic("+/+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalseGivenSingleLevelWildcardAsPartOfLevel()
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