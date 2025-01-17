using Net.Mqtt.Exceptions;
using Map = Net.Mqtt.AliasTopicMap;

namespace Net.Mqtt.Tests.AliasTopicMap;

[TestClass]
public class GetOrUpdateTopicShould
{
    [TestMethod]
    public void ThrowInvalidTopicAliasException_GivenAliasZero()
    {
        var map = new Map();
        map.Initialize(ushort.MaxValue);
        ReadOnlyMemory<byte> topic = default;

        Assert.ThrowsException<InvalidTopicAliasException>(() => map.GetOrUpdateTopic(0, ref topic));
    }

    [TestMethod]
    public void ThrowInvalidTopicAliasException_GivenAliasGreaterMaxTopicAlias()
    {
        var map = new Map();
        map.Initialize(5);
        ReadOnlyMemory<byte> topic = default;

        Assert.ThrowsException<InvalidTopicAliasException>(() => map.GetOrUpdateTopic(6, ref topic));
    }

    [TestMethod]
    public void ThrowProtocolErrorException_GivenNewAliasAndEmptyTopicRef()
    {
        var map = new Map();
        map.Initialize(5);
        ReadOnlyMemory<byte> topic = default;

        Assert.ThrowsException<ProtocolErrorException>(() => map.GetOrUpdateTopic(1, ref topic));
    }

    [TestMethod]
    public void InsertNewMapping_GivenNewAliasAndNonEmptyTopicRef()
    {
        ReadOnlyMemory<byte> topic1 = "Lorem/ipsum/dolor/sit/amet/consectetur/adipiscing/elit"u8.ToArray();
        var map = new Map();
        map.Initialize(5);

        // Verify mapping doesn't exist
        Assert.ThrowsException<ProtocolErrorException>(() =>
        {
            ReadOnlyMemory<byte> actual = default;
            map.GetOrUpdateTopic(1, ref actual);
        });

        // Insert new mapping 1:topic1 and verify that original passed ref is not affected
        var actual = topic1;
        map.GetOrUpdateTopic(1, ref topic1);

        Assert.That.AreSameRef(topic1, actual);

        // Verify new mapping has been added and now points to the topic1
        actual = default;
        map.GetOrUpdateTopic(1, ref actual);

        Assert.That.AreSameRef(topic1, actual);
    }

    [TestMethod]
    public void ReturnExistingMapping_GivenExistingAliasAndEmptyTopicRef()
    {
        ReadOnlyMemory<byte> topic1 = "Lorem/ipsum/dolor/sit/amet/consectetur/adipiscing/elit"u8.ToArray();
        var map = new Map();
        map.Initialize(5);
        map.GetOrUpdateTopic(1, ref topic1);

        ReadOnlyMemory<byte> actual = default;
        map.GetOrUpdateTopic(1, ref actual);

        Assert.That.AreSameRef(topic1, actual);
    }

    [TestMethod]
    public void UpdateExistingMapping_GivenExistingAliasAndNotEmptyTopic()
    {
        ReadOnlyMemory<byte> topic1 = "Lorem/ipsum/dolor/sit/amet/consectetur/adipiscing/elit"u8.ToArray();
        ReadOnlyMemory<byte> topic2 = "dolore/eu/fugiat/nulla/pariatur"u8.ToArray();
        var map = new Map();
        map.Initialize(5);
        map.GetOrUpdateTopic(1, ref topic1);

        // Verify existing mapping has reference to topic1
        ReadOnlyMemory<byte> actual = default;
        map.GetOrUpdateTopic(1, ref actual);

        Assert.That.AreSameRef(topic1, actual);

        // Update existing mapping with new ref to the topic2
        map.GetOrUpdateTopic(1, ref topic2);

        // Verify mapping has been updated to the new ref topic2
        actual = default;
        map.GetOrUpdateTopic(1, ref actual);

        Assert.That.AreSameRef(topic2, actual);
    }
}