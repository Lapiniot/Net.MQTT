using Map = Net.Mqtt.TopicAliasMap;

namespace Net.Mqtt.Tests.TopicAliasMap;

[TestClass]
public class InitializeShould
{
    [TestMethod]
    public void SetupInitialState_OtherMethodsThrow_UnlessInitializeInvokedBefore()
    {
        var map = new Map();
        var topic = "topic1"u8.ToArray();

        Assert.ThrowsExactly<NullReferenceException>(() => map.TryGetAlias(topic, out _, out _));

        Assert.ThrowsExactly<NullReferenceException>(() =>
        {
            (ReadOnlyMemory<byte> topic, ushort) mapping = (topic, 1);
            map.Commit(ref mapping);
        });
    }

    [TestMethod]
    public void AllowUpToMaximumMappings_GivenValidAliasMaximum()
    {
        var map = new Map();

        map.Initialize(10);

        // Verify initialization - should be able to get aliases up to 10
        for (var i = 1; i <= 10; i++)
        {
            var topic = UTF8.GetBytes($"test/topic{i}");
            Assert.IsTrue(map.TryGetAlias(topic, out var mapping, out var needsCommit));
            Assert.IsTrue(needsCommit);
            CollectionAssert.AreEqual(topic, mapping.Topic);
            Assert.AreEqual(i, mapping.Alias);

            map.Commit(ref mapping);
        }

        // Next should return false indicating that we reached maximum
        Assert.IsFalse(map.TryGetAlias(UTF8.GetBytes($"test/topic11"), out _, out _));
    }

    [TestMethod]
    public void PreventAnyMappings_GivenAliasMaximumZero()
    {
        var map = new Map();
        var topic = "test/topic"u8.ToArray();

        map.Initialize(0); // No aliases allowed

        Assert.IsFalse(map.TryGetAlias(topic, out var mapping, out var needsCommit));
        Assert.AreEqual(default, mapping);
        Assert.IsFalse(needsCommit);
    }

    [TestMethod]
    public void ResetState()
    {
        var map = new Map();
        map.Initialize(5);
        var topic = "test/topic"u8.ToArray();

        // Add a mapping
        (ReadOnlyMemory<byte> Topic, ushort Alias) mapping = (topic, 1);
        map.Commit(ref mapping);

        // Verify mapping was permanently added to the state with alias = 1
        Assert.IsTrue(map.TryGetAlias(topic, out mapping, out var needsCommit));
        Assert.IsFalse(needsCommit);
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        // Re-initialize and add other topic permanently
        map.Initialize(2);
        var otherTopic = "test/other-topic"u8.ToArray();
        mapping = (otherTopic, 1);
        map.Commit(ref mapping);

        // Verify this other topic now gets associated with alias = 1
        Assert.IsTrue(map.TryGetAlias(otherTopic, out mapping, out needsCommit));
        Assert.IsFalse(needsCommit);
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        // Now should get ephemeral mapping with different alias value for the original topic
        Assert.IsTrue(map.TryGetAlias(topic, out mapping, out needsCommit));
        Assert.IsTrue(needsCommit);
        CollectionAssert.AreEqual(topic, mapping.Topic);
        Assert.AreEqual(2, mapping.Alias);
    }
}