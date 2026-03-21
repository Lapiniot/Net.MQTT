using System.Runtime.CompilerServices;
using Map = Net.Mqtt.TopicAliasMap;
using Mapping = (System.ReadOnlyMemory<byte> Topic, ushort Alias);

namespace Net.Mqtt.Tests.TopicAliasMap;

[TestClass]
public class CommitShould
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "nextAlias")]
    private static extern ref int GetNextAliasField(ref Map map);

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenEmptyTopic()
    {
        var map = new Map();
        map.Initialize(1);

        Mapping mapping = (default, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            action: () => map.Commit(ref mapping),
            message: "mapping.Topic.Length ('0') must be a non-zero value.");
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenAliasZero()
    {
        var map = new Map();
        map.Initialize(1);

        Mapping mapping = ("topic"u8.ToArray(), 0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            action: () => map.Commit(ref mapping),
            message: "mapping.Alias ('0') must be a non-zero value.");
    }

    [TestMethod]
    public void Commit_MakeTopicMappingPermanent()
    {
        var map = new Map();
        map.Initialize(10);
        var topic = "test/topic"u8.ToArray();

        // Get first alias
        Assert.IsTrue(map.TryGetAlias(topic, out var mapping, out var newNeedsCommit));
        Assert.IsTrue(newNeedsCommit);
        CollectionAssert.AreEqual(topic, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        map.Commit(ref mapping);

        Assert.IsTrue(map.TryGetAlias(topic, out mapping, out newNeedsCommit));
        Assert.IsFalse(newNeedsCommit); // Clearly indicates mapping was permanently added
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);
    }

    [TestMethod]
    public void Commit_IncrementNextAliasCounter()
    {
        var map = new Map();
        map.Initialize(10);
        var topic = "test/topic"u8.ToArray();

        // Get first alias
        Assert.IsTrue(map.TryGetAlias(topic, out var mapping, out _));
        Assert.AreEqual(1, mapping.Alias);
        map.Commit(ref mapping);

        // Next suggested alias should be 2
        var topic2 = "test/topic2"u8.ToArray();
        Assert.IsTrue(map.TryGetAlias(topic2, out mapping, out _));
        Assert.AreEqual(2, mapping.Alias);
    }

    [TestMethod]
    [DataRow((ushort)10, DisplayName = "Small number, no overflow on UInt16 math is expected at all.")]
    [DataRow((ushort)short.MaxValue, DisplayName = "Edge case: maximum value for Int16, will become negative after increment if somewhere interpreted as signed Int16.")]
    [DataRow(ushort.MaxValue, DisplayName = "Edge case: maximum value for UInt16, may overflow after increment and become 0.")]
    public void ThrowInvalidOperationException_DoNotCorruptInternalState_GivenExhaustedMap(ushort aliasMaximum)
    {
        var map = new Map();
        map.Initialize(aliasMaximum);

        // Simulate near-exhaustion state, use UnsafeAccessor to modify field directly - just for performance reasons
        ref var nextAlias = ref GetNextAliasField(ref map);
        nextAlias = aliasMaximum;
        // Commit to insert last allowed permanent mapping.
        Mapping mapping = (UTF8.GetBytes($"t{aliasMaximum:D5}"), (ushort)nextAlias);
        map.Commit(ref mapping);

        // This commit should fail, because map is alredy exhausted
        var topic = UTF8.GetBytes($"t{aliasMaximum + 1:D5}");
        mapping = (topic, aliasMaximum);
        Assert.ThrowsExactly<InvalidOperationException>(
            action: () => map.Commit(ref mapping),
            message: "Topic alias map is exhausted.");

        // Verify last failed operation doesn't leave orphaned semi-commited mapping in the map 
        // before throwing for that last topic commit over limit
        Assert.IsFalse(map.TryGetAlias(topic, out mapping, out var newNeedsCommit));
        Assert.IsFalse(newNeedsCommit);
        Assert.AreEqual(default, mapping);

        // Verify that internal state is correctly maintained, there was no state corruption issue due to the 
        // counter overflow, boundary limit check works as expected, so TryGetAlias gracefully fails 
        // reporting that map is exhausted and cannot serve new mappings anymore
        Assert.IsFalse(map.TryGetAlias("some-topic"u8.ToArray(), out mapping, out newNeedsCommit));
        Assert.IsFalse(newNeedsCommit);
        Assert.AreEqual(default, mapping);
    }

    [TestMethod]
    public void ThrowInvalidOperationException_GivenAliasAlreadyInUse()
    {
        var map = new Map();
        map.Initialize(10);
        var topic1 = "test/topic1"u8.ToArray();
        var topic2 = "test/topic2"u8.ToArray();

        // Get two ephemeral mappings, both are still temporal and have the same suggested alias value
        map.TryGetAlias(topic1, out var mapping1, out _);
        map.TryGetAlias(topic2, out var mapping2, out _);
        // Verify both mapping pairs get the same suggested alias value
        Assert.AreEqual(1, mapping1.Alias);
        Assert.AreEqual(1, mapping2.Alias);

        // Commit second ephemeral mapping before the first one
        map.Commit(ref mapping2);

        // Verify it is now permanent (first commited wins)
        Assert.IsTrue(map.TryGetAlias(topic2, out mapping2, out var newNeedsCommit));
        Assert.IsFalse(newNeedsCommit);
        CollectionAssert.AreEqual(default, mapping2.Topic);
        Assert.AreEqual(1, mapping2.Alias);

        // Now try to commit first ephemeral mapping with alias == 1 (which is already associated with 'test/topic2')
        Assert.ThrowsExactly<InvalidOperationException>(
            action: () => map.Commit(ref mapping1),
            message: "Specified alias is already in use for another topic mapping.");
    }

    [TestMethod]
    public void ThrowInvalidOperationException_GivenDifferentAliasForExistingTopic()
    {
        var map = new Map();
        map.Initialize(10);
        var topic = "test/topic1"u8.ToArray();

        map.TryGetAlias(topic, out var mapping, out _);
        map.Commit(ref mapping);

        // Verify it is now permanent and associated with alias == 1
        Assert.IsTrue(map.TryGetAlias(topic, out mapping, out var newNeedsCommit));
        Assert.IsFalse(newNeedsCommit);
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        // Now try to commit the same permanent topic with another alias == 2
        mapping = (topic, 2);
        Assert.ThrowsExactly<InvalidOperationException>(
            action: () => map.Commit(ref mapping),
            message: "Alias remapping for existing topic is not supported.");
    }

    [TestMethod]
    public void BeIdempotent_GivenSameAliasSameTopic_CalledMultipleTimes()
    {
        var map = new Map();
        map.Initialize(10);
        var topic1 = "test/topic"u8.ToArray();
        var topic2 = "test/topic2"u8.ToArray();

        // First commit:
        // Verify there is no permanent mapping yet for given topic
        Assert.IsTrue(map.TryGetAlias(topic1, out var mapping, out var needsCommit));
        Assert.IsTrue(needsCommit);
        CollectionAssert.AreEqual(topic1, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        // Commit topic to make it permanent
        map.Commit(ref mapping);

        // Verify topic/alias mapping is now permanently registered
        Assert.IsTrue(map.TryGetAlias(topic1, out mapping, out needsCommit));
        Assert.IsFalse(needsCommit);
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        // Verify current alias counter was advanced by prev. call to Commit() 
        Assert.IsTrue(map.TryGetAlias(topic2, out mapping, out _));
        Assert.AreEqual(2, mapping.Alias);

        // Second commit on same topic
        mapping = (topic1, 1);
        map.Commit(ref mapping);

        // Should not change anything for already commited topic
        Assert.IsTrue(map.TryGetAlias(topic1, out mapping, out needsCommit));
        Assert.IsFalse(needsCommit); // Already exists
        CollectionAssert.AreEqual(default, mapping.Topic);
        Assert.AreEqual(1, mapping.Alias);

        // Verify current alias counter remains the same and wasn't advanced 
        // by prev. call to Commit() for already commited mapping. This assert essentially 
        // proves that internal state is not affected by idempotent Commit() operation.
        Assert.IsTrue(map.TryGetAlias(topic2, out mapping, out _));
        Assert.AreEqual(2, mapping.Alias);
    }
}