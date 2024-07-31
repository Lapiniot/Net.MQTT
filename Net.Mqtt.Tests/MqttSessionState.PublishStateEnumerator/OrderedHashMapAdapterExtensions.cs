#if !NET9_0_OR_GREATER
namespace Net.Mqtt.Tests.MqttSessionState.PublishStateEnumerator;

internal static class OrderedHashMapAdapterExtensions
{
    public static void Add(this PublishStateMap hashMap, ushort key, string value) => hashMap.AddOrUpdate(key, value);
}
#endif