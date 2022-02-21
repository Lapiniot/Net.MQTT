using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt;

internal readonly record struct PacketBlock(ushort Id, byte Flags, string Topic, in ReadOnlyMemory<byte> Payload);

public delegate void PubRelDispatchHandler(ushort id);

public delegate void PublishDispatchHandler(ushort id, byte flags, string topic, in ReadOnlyMemory<byte> payload);

public abstract class MqttSessionState
{
    private readonly IdentityPool idPool;
    private readonly HashSet<ushort> receivedQos2;
    private readonly OrderedHashMap<ushort, PacketBlock> resendMap;

    protected MqttSessionState()
    {
        receivedQos2 = new();
        resendMap = new(); //TODO: investigate performance with explicit capacity initially set here
        idPool = new FastIdentityPool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort RentId() => idPool.Rent();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnId(ushort id) => idPool.Release(id);

    public bool TryAddQoS2(ushort packetId) => receivedQos2.Add(packetId);

    public bool RemoveQoS2(ushort packetId) => receivedQos2.Remove(packetId);

    public ushort AddPublishToResend(byte flags, string topic, in ReadOnlyMemory<byte> payload)
    {
        var id = idPool.Rent();
        var message = new PacketBlock(id, (byte)(flags | Duplicate), topic, in payload);
        resendMap.AddOrUpdate(id, message, message);
        return id;
    }

    public void AddPubRelToResend(ushort id)
    {
        var message = new PacketBlock { Id = id };
        resendMap.AddOrUpdate(id, message, message);
    }

    public bool RemoveFromResend(ushort id)
    {
        if (!resendMap.TryRemove(id, out _)) return false;
        idPool.Release(id);
        return true;
    }

    public void DispatchPendingMessages([NotNull] PubRelDispatchHandler pubRelHandler, [NotNull] PublishDispatchHandler publishHandler)
    {
        // TODO: consider using Parallel.Foreach
        foreach (var (id, flags, topic, payload) in resendMap)
        {
            if (topic is null)
            {
                pubRelHandler(id);
            }
            else
            {
                publishHandler(id, flags, topic, payload);
            }
        }
    }

    public virtual void Trim()
    {
        receivedQos2.TrimExcess();
        resendMap.TrimExcess();
    }
}