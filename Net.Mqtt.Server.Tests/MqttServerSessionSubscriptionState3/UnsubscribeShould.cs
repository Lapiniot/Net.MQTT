namespace Net.Mqtt.Server.Tests.MqttServerSessionSubscriptionState3;

[TestClass]
public class UnsubscribeShould
{
    [TestMethod]
    public void RemoveSubscriptions_GivenExistingFilters()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();
        state.Subscribe([("#"u8.ToArray(), 0x01)], out _);

        // Act
        state.Unsubscribe(["#"u8.ToArray()], out var currentCount);

        // Assert
        Assert.AreEqual(0, currentCount);
    }

    [TestMethod]
    public void Noop_GivenNonExistingFilters()
    {
        // Arrange
        var state = new Protocol.V3.MqttServerSessionSubscriptionState3();
        state.Subscribe([("#"u8.ToArray(), 0x01)], out _);

        // Act
        state.Unsubscribe(["topic1/#"u8.ToArray()], out var currentCount);

        // Assert
        Assert.AreEqual(1, currentCount);
    }
}