using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Mqtt.MqttHelpersTests
{
    [TestClass]
    public class MqttHelpers_IsValidTopic_Should
    {
        [TestMethod]
        public void ReturnFalse_GivenNullTopic()
        {
            var actual = MqttHelpers.IsValidTopic(null);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenEmptyTopic()
        {
            var actual = MqttHelpers.IsValidTopic(string.Empty);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcardOnly()
        {
            var actual = MqttHelpers.IsValidTopic("#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenSingleLevelWildcardOnly()
        {
            var actual = MqttHelpers.IsValidTopic("+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenLevelSeparatorOnly()
        {
            var actual = MqttHelpers.IsValidTopic("/");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultiLevelWildcardAtLastLevel()
        {
            var actual = MqttHelpers.IsValidTopic("a/#");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenMultiLevelWildcardAtNotLastLevel()
        {
            var actual = MqttHelpers.IsValidTopic("a/#/b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenMultiLevelWildcardAsPartOfLevel()
        {
            var actual = MqttHelpers.IsValidTopic("a/b#");
            Assert.IsFalse(actual);

            actual = MqttHelpers.IsValidTopic("a/#b");
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenSingleLevelWildcardAtAnyLevel()
        {
            var actual = MqttHelpers.IsValidTopic("+/a/b");
            Assert.IsTrue(actual);

            actual = MqttHelpers.IsValidTopic("a/+/b");
            Assert.IsTrue(actual);

            actual = MqttHelpers.IsValidTopic("a/b/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnTrue_GivenMultipleSingleLevelWildcards()
        {
            var actual = MqttHelpers.IsValidTopic("+/a/+");
            Assert.IsTrue(actual);

            actual = MqttHelpers.IsValidTopic("+/+/+");
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ReturnFalse_GivenSingleLevelWildcardAsPartOfLevel()
        {
            var actual = MqttHelpers.IsValidTopic("a/b+");
            Assert.IsFalse(actual);

            actual = MqttHelpers.IsValidTopic("a/b+/");
            Assert.IsFalse(actual);

            actual = MqttHelpers.IsValidTopic("a/+b");
            Assert.IsFalse(actual);

            actual = MqttHelpers.IsValidTopic("a/+b/");
            Assert.IsFalse(actual);
        }
    }
}