namespace Net.Mqtt.Server;

#if !NET10_0_OR_GREATER
[InlineArray(16)]
internal struct InlineArray16<T>
{
#pragma warning disable IDE0051, IDE0044
    private T item0;
#pragma warning restore IDE0051, IDE0044
}
#endif