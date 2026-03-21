using Map = Net.Mqtt.TopicAliasMap;

namespace Net.Mqtt.Tests.TopicAliasMap;

[TestClass]
public class TryGetAliasShould
{
    [TestMethod]
    public void ReturnTrue_ExistingMapping_GivenTopicAlreadyMapped()
    {
        var map = new Map();
        map.Initialize(10);
        var topic = "test/topic"u8.ToArray();

        // First call should create new mapping
        Assert.IsTrue(map.TryGetAlias(topic, out var mapping, out var needsCommit));
        Assert.IsTrue(needsCommit);
        CollectionAssert.AreEqual(topic, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        // Commit the mapping
        map.Commit(ref mapping);

        // Second call should return existing mapping
        Assert.IsTrue(map.TryGetAlias(topic, out mapping, out needsCommit));
        Assert.IsFalse(needsCommit);
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);
    }

    [TestMethod]
    public void ReturnTrue_NewMapping_GivenTopicNotMappedAndAliasesAvailable()
    {
        var map = new Map();
        map.Initialize(5);
        var topic1 = "topic1"u8.ToArray();
        var topic2 = "topic2"u8.ToArray();

        // First topic
        Assert.IsTrue(map.TryGetAlias(topic1, out var mapping, out var needsCommit));
        Assert.IsTrue(needsCommit);
        CollectionAssert.AreEqual(topic1, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);
        map.Commit(ref mapping);

        // Second topic
        Assert.IsTrue(map.TryGetAlias(topic2, out mapping, out needsCommit));
        Assert.IsTrue(needsCommit);
        CollectionAssert.AreEqual(topic2, mapping.Topic);
        Assert.AreEqual(2, mapping.Alias);
    }

    [TestMethod]
    public void ReturnFalse_GivenMapExhaustedNoFreeAlieses()
    {
        var map = new Map();
        map.Initialize(1); // Only one alias available
        var topic1 = "topic1"u8.ToArray();
        var topic2 = "topic2"u8.ToArray();

        // Use up to one alias
        Assert.IsTrue(map.TryGetAlias(topic1, out var mapping, out var needsCommit));
        Assert.IsTrue(needsCommit);
        CollectionAssert.AreEqual(topic1, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);
        map.Commit(ref mapping);

        // Next topic should fail
        Assert.IsFalse(map.TryGetAlias(topic2, out mapping, out needsCommit));
        Assert.IsFalse(needsCommit);
        Assert.AreEqual(default, mapping);
    }

    [TestMethod]
    [Description("""
    This test essentially verifies there is no extra validation for empty topics 
    and such test might look a bit contraversal. Although, it is absolutely pointless 
    to keep empty topic in the map, we do not perform extra checks and method never throws. 
    This extra validation is omited on purpose, primarily for performance reasons, because method 
    is called frequently on hot-path. The situation when we can get to the empty topic passed to the 
    method is practically impossible in the current use-case scenario. 
    Morover, such empty topic will be rejected by Commit() anyway.
""")]
    public void ReturnTrue_GivenEmptyTopic()
    {
        var map = new Map();
        map.Initialize(5);

        // Empty topic should still be mappable
        Assert.IsTrue(map.TryGetAlias(topic: ReadOnlyMemory<byte>.Empty, out var mapping, out var needsCommit));
        Assert.IsTrue(needsCommit);
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);
    }
}