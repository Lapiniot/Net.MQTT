using System.Collections.Immutable;

namespace Net.Mqtt.Server.Tests.MqttServerSessionSubscriptionState5;

[TestClass]
public class SubscribeShould
{

    [TestMethod]
    public void ReturnResultCodesAndSubscriptionMetadata_GivenValidFilters()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        var filters = new List<(byte[] Filter, byte Flags)>
        {
            ("testtopic1/#"u8.ToArray(), 0b0010_0000), // QoS 0
            ("testtopic2/#"u8.ToArray(), 0b0011_0001), // QoS 1
            ("testtopic3/#"u8.ToArray(), 0b0001_0010), // QoS 2
        };

        // Act
        var result = target.Subscribe(filters, 16);

        // Assert
        CollectionAssert.AreEqual((ImmutableArray<byte>)[0x00, 0x01, 0x02], result.Feedback);
        Assert.AreEqual(3, result.TotalCount);
        Assert.HasCount(3, result.Subscriptions);

        CollectionAssert.AreEqual(filters[0].Filter, result.Subscriptions[0].Filter);
        Assert.IsFalse(result.Subscriptions[0].Exists);
        Assert.AreEqual(new(0x00, 0b0010_0000, 16), result.Subscriptions[0].Options);

        CollectionAssert.AreEqual(filters[1].Filter, result.Subscriptions[1].Filter);
        Assert.IsFalse(result.Subscriptions[1].Exists);
        Assert.AreEqual(new(0x01, 0b0011_0001, 16), result.Subscriptions[1].Options);

        CollectionAssert.AreEqual(filters[2].Filter, result.Subscriptions[2].Filter);
        Assert.IsFalse(result.Subscriptions[2].Exists);
        Assert.AreEqual(new(0x02, 0b0001_0010, 16), result.Subscriptions[2].Options);
    }

    [TestMethod]
    public void ReturnResultCodesButNoSubscriptionMetadata_GivenInvalidFilters()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();
        var filters = new List<(byte[] Filter, byte Flags)>
        {
            ("testtopic1/#/123"u8.ToArray(), 0b0010_0000),  // Invalid topic filter
            ("testtopic2/#"u8.ToArray(), 0b0011_0011),      // Invalid flags
        };

        // Act
        var result = target.Subscribe(filters, 0);

        // Assert
        CollectionAssert.AreEqual((ImmutableArray<byte>)[0x80, 0x80], result.Feedback);
        Assert.AreEqual(0, result.TotalCount);
        Assert.HasCount(0, result.Subscriptions);
    }

    [TestMethod]
    public void UpdateExistingSubscription_GivenDuplicateFilters()
    {
        // Arrange
        var target = new Protocol.V5.MqttServerSessionSubscriptionState5();

        target.Subscribe([
            ("testtopic1/#"u8.ToArray(), 0b0001_0001) // QoS 1
        ], 1);

        // Act
        var result = target.Subscribe([
            ("testtopic1/#"u8.ToArray(), 0b0001_0010) // QoS 2 (upgrade)
        ], 2);

        // Assert
        Assert.AreEqual(0x02, result.Feedback[0]);
        Assert.AreEqual(1, result.TotalCount);
        Assert.IsTrue(result.Subscriptions[0].Exists);
        Assert.AreEqual(0x02, result.Subscriptions[0].Options.QoS);
        Assert.AreEqual(0b0001_0010, result.Subscriptions[0].Options.Flags);
        Assert.AreEqual(2u, result.Subscriptions[0].Options.SubscriptionId);
    }
}