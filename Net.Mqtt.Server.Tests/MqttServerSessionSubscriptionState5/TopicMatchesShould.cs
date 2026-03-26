namespace Net.Mqtt.Server.Tests.MqttServerSessionSubscriptionState5;

[TestClass]
public class TopicMatchesShould
{
    [TestMethod]
    public void ReturnTrue_MatchingSubscriptionMetadata_GivenMatchingTopic()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        List<(byte[] Filter, byte Flags)> filters = [
            ("testtopic1/#"u8.ToArray(), 0x01), // QoS 1
            ("testtopic2/#"u8.ToArray(), 0x02)  // QoS 2
        ];
        target.Subscribe(filters, 10u);

        // Act
        var topic = "testtopic1"u8;
        var result = target.TopicMatches(topic, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(new(0x01, 0x01, 10u), options);
    }

    [TestMethod]
    public void ReturnTrue_SubscriptionIdsNull_GivenMatchingTopic_NoSubscriptionIdAssociated()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        List<(byte[] Filter, byte Flags)> filters = [
            ("testtopic1/#"u8.ToArray(), 0x01)  // QoS 2
        ];
        target.Subscribe(filters, 0);

        // Act
        var topic = "testtopic1"u8;
        var result = target.TopicMatches(topic, out var options, out var subscriptionIds);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(new(0x01, 0x01, 0), options);
        Assert.IsNull(subscriptionIds);
    }

    [TestMethod]
    public void ReturnTrue_HighestQoS_AllMatchingSubscriptionIds_GivenMultipleMatchingSubscriptions()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        target.Subscribe([("testtopic/#"u8.ToArray(), 0x00)], 0);
        target.Subscribe([("testtopic/+"u8.ToArray(), 0x02)], 0);
        target.Subscribe([("testtopic/1"u8.ToArray(), 0x01)], 0);
        target.Subscribe([("testtopic"u8.ToArray(), 0x01)], 0);

        // Act
        var result = target.TopicMatches("testtopic/1"u8, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0x02, options.QoS);
    }

    [TestMethod]
    public void ReturnTrue_TrackSubscriptionIds_GivenMultipleMatchingSubscriptionsWithIds()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        target.Subscribe([("testtopic/#"u8.ToArray(), 0x00)], 10u);
        target.Subscribe([("testtopic/+"u8.ToArray(), 0x02)], 30u);
        target.Subscribe([("testtopic/1"u8.ToArray(), 0x01)], 20u);
        target.Subscribe([("testtopic/1/#"u8.ToArray(), 0x01)], 0);

        // Act
        var result = target.TopicMatches("testtopic/1"u8, out var options, out var subscriptionIds);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(30u, options.SubscriptionId);
        Assert.IsNotNull(subscriptionIds);
        CollectionAssert.AreEquivalent((uint[])[10u, 30u, 20u], subscriptionIds.ToArray());
    }

    [TestMethod]
    public void ReturnFalse_GivenNoMatch()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        target.Subscribe([
            ("testtopic1/#"u8.ToArray(), 0x00),
            ("testtopic2/#"u8.ToArray(), 0x00),
            ("testtopic3/#"u8.ToArray(), 0x00)
        ], 0);

        // Act
        var result = target.TopicMatches("testtopic4"u8, out var options, out var subscriptionIds);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(default, options);
        Assert.IsNull(subscriptionIds);
    }
}