namespace Net.Mqtt.Tests.MqttSessionState.PublishStateEnumerator;

[TestClass]
public class DisposeShould
{
    [TestMethod]
    public void Complete_Enumerator_Immediately_Being_Called_Before_First_MoveNext()
    {
        var map = new PublishStateMap { { 0, "state 0" }, { 1, "state 1" } };
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();

        // Act
        enumerable.Dispose();

        // Assert
        var actual = enumerable.MoveNext();
        Assert.IsFalse(actual);
        Assert.AreEqual(default, enumerable.Current);
    }

    [TestMethod]
    public void Complete_Progressing_Enumerator_And_Unlock()
    {
        var map = new PublishStateMap { { 0, "state 0" }, { 1, "state 1" } };
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();
        enumerable.MoveNext();

        // Act
        var lockedBeforeDispose = Monitor.IsEntered(map);
        enumerable.Dispose();
        var lockedAfterDispose = Monitor.IsEntered(map);

        // Assert

        var actual = enumerable.MoveNext();
        Assert.IsFalse(actual);
        Assert.AreEqual(new(0, "state 0"), enumerable.Current);

        Assert.IsTrue(lockedBeforeDispose);
        Assert.IsFalse(lockedAfterDispose);
    }

    [TestMethod]
    public void DoesNot_Throw_On_Subsequent_Calls()
    {
        var map = new PublishStateMap { { 0, "state 0" }, { 1, "state 1" } };
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();
        enumerable.MoveNext();

        // Act
        enumerable.Dispose();
        enumerable.Dispose();
    }

    [TestMethod]
    public void DoesNot_Throw_On_Completed_Enumerator()
    {
        var map = new PublishStateMap { { 0, "state 0" }, { 1, "state 1" } };
        var enumerator = new MqttSessionState<string>.PublishStateEnumerator(map);
        using var enumerable = enumerator.GetEnumerator();
        enumerable.MoveNext();
        enumerable.MoveNext();
        enumerable.MoveNext();

        // Act
        enumerable.Dispose();
    }
}
