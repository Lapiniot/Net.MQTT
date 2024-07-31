using Microsoft.VisualStudio.TestTools.UnitTesting;
using OOs.Collections.Generic;

namespace Net.Mqtt.Tests.MqttSessionState.PublishStateEnumerator;

[TestClass]
public class MoveNextShould
{
    [TestMethod]
    public void Return_False_Given_EmptyState_Before_GetEnumerator_Called()
    {
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator([]);

        var actual = enumerator.MoveNext();

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void Return_False_Given_EmptyState_After_GetEnumerator_Called()
    {
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator([]);
        using var enumerable = enumerator.GetEnumerator();

        var actual = enumerable.MoveNext();

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void Return_False_DoesNot_Move_Given_NonEmptyState_Before_GetEnumerator_Called()
    {
        var map = new OrderedHashMap<ushort, string>();
        map.AddOrUpdate(0, "state 0");
        map.AddOrUpdate(1, "state 1");
        map.AddOrUpdate(2, "state 2");
        map.AddOrUpdate(3, "state 3");
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);

        var actual = enumerator.MoveNext();

        Assert.IsFalse(actual);
        Assert.AreEqual(default, enumerator.Current);
    }

    [TestMethod]
    public void Return_True_Moves_To_Next_Item_When_Called_Before_Last_Item_Reached()
    {
        var map = new OrderedHashMap<ushort, string>();
        map.AddOrUpdate(0, "state 0");
        map.AddOrUpdate(1, "state 1");
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();

        var actual = enumerable.MoveNext();
        Assert.IsTrue(actual);
        Assert.AreEqual(new(0, "state 0"), enumerable.Current);

        actual = enumerable.MoveNext();
        Assert.IsTrue(actual);
        Assert.AreEqual(new(1, "state 1"), enumerable.Current);
    }

    [TestMethod]
    public void Return_False_DoesNot_Move_To_Next_Item_When_Called_After_Last_Item_Reached()
    {
        var map = new OrderedHashMap<ushort, string>();
        map.AddOrUpdate(0, "state 0");
        map.AddOrUpdate(1, "state 1");
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();
        enumerable.MoveNext();
        enumerable.MoveNext();

        var actual = enumerable.MoveNext();
        Assert.IsFalse(actual);
        Assert.AreEqual(new(1, "state 1"), enumerable.Current);
    }

    [TestMethod]
    public void Return_False_DoesNot_Move_To_Next_Item_When_Called_After_Dispose()
    {
        var map = new OrderedHashMap<ushort, string>();
        map.AddOrUpdate(0, "state 0");
        map.AddOrUpdate(1, "state 1");
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();
        enumerable.MoveNext();
        enumerable.Dispose();

        var actual = enumerable.MoveNext();
        Assert.IsFalse(actual);
        Assert.AreEqual(new(0, "state 0"), enumerable.Current);
    }

    [TestMethod]
    public void Acquire_Lock_On_Underlaying_Map_On_First_Call()
    {
        var map = new OrderedHashMap<ushort, string>();
        map.AddOrUpdate(0, "state 0");
        map.AddOrUpdate(1, "state 1");
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();

        Assert.IsFalse(Monitor.IsEntered(map));

        enumerable.MoveNext();

        Assert.IsTrue(Monitor.IsEntered(map));
    }

    [TestMethod]
    public void Release_Lock_On_Underlaying_Map_On_Complete()
    {
        var map = new OrderedHashMap<ushort, string>();
        map.AddOrUpdate(0, "state 0");
        map.AddOrUpdate(1, "state 1");
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();
        enumerable.MoveNext();
        enumerable.MoveNext();

        enumerable.MoveNext();
        Assert.IsFalse(Monitor.IsEntered(map));
    }
}