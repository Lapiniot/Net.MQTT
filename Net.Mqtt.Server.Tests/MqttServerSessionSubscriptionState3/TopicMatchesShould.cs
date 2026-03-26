namespace Net.Mqtt.Server.Tests.MqttServerSessionSubscriptionState3;

[TestClass]
public class TopicMatchesShould
{
    [TestMethod]
    public void ReturnTrue_GivenMatchingTopic()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();
        state.Subscribe([("testtopic1/#"u8.ToArray(), 0x01)], out _);

        // Act
        var topic = "testtopic1"u8;
        var result = state.TopicMatches(topic, out var maxQoS);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(QoSLevel.QoS1, maxQoS);
    }

    [TestMethod]
    public void ReturnFalse_GivenNoMatch()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();
        state.Subscribe([("testtopic1/#"u8.ToArray(), 0x01)], out _);

        // Act
        var topic = "testtopic4"u8;
        var result = state.TopicMatches(topic, out var maxQoS);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(default, maxQoS);
    }

    [TestMethod]
    public void ReturnTrue_HighestQoS_GivenMultipleMatchingSubscriptions()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();
        state.Subscribe([("testtopic/#"u8.ToArray(), 0x00)], out _);
        state.Subscribe([("testtopic/+"u8.ToArray(), 0x02)], out _);
        state.Subscribe([("testtopic/1"u8.ToArray(), 0x01)], out _);

        // Act
        var result = state.TopicMatches("testtopic/1"u8, out var maxQoS);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(QoSLevel.QoS2, maxQoS);
    }
}