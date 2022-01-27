using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Net.Mqtt.PacketFlags;

namespace System.Net.Mqtt;

internal readonly record struct PacketBlock(ushort Id, byte Flags, string Topic, in ReadOnlyMemory<byte> Payload);
public delegate void PubRelDispatchHandler(ushort id);
public delegate void PublishDispatchHandler(ushort id, byte flags, string topic, in ReadOnlyMemory<byte> payload);

public abstract class MqttSessionState : IDisposable
{
    private readonly HashSet<ushort> receivedQos2;
    private readonly IdentityPool idPool;
    private readonly HashQueueCollection<ushort, PacketBlock> resendQueue;
    private bool disposed;

    protected MqttSessionState()
    {
        receivedQos2 = new HashSet<ushort>();
        resendQueue = new HashQueueCollection<ushort, PacketBlock>();
        idPool = new FastIdentityPool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort RentId()
    {
        return idPool.Rent();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnId(ushort id)
    {
        idPool.Release(id);
    }

    public bool TryAddQoS2(ushort packetId)
    {
        return receivedQos2.Add(packetId);
    }

    public bool RemoveQoS2(ushort packetId)
    {
        return receivedQos2.Remove(packetId);
    }

    public ushort AddPublishToResend(byte flags, string topic, in ReadOnlyMemory<byte> payload)
    {
        var id = idPool.Rent();
        var message = new PacketBlock(id, (byte)(flags | Duplicate), topic, in payload);
        resendQueue.AddOrUpdate(id, message, message);
        return id;
    }

    public void AddPubRelToResend(ushort id)
    {
        var message = new PacketBlock { Id = id };
        resendQueue.AddOrUpdate(id, message, message);
    }

    public bool RemoveFromResend(ushort id)
    {
        if(!resendQueue.TryRemove(id, out _)) return false;
        idPool.Release(id);
        return true;
    }

    public void DispatchPendingMessages([NotNull] PubRelDispatchHandler pubRelHandler, [NotNull] PublishDispatchHandler publishHandler)
    {
        // TODO: consider using Parallel.Foreach
        foreach(var (id, flags, topic, payload) in resendQueue)
        {
            if(topic is null)
            {
                pubRelHandler(id);
            }
            else
            {
                publishHandler(id, flags, topic, payload);
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if(disposed) return;
        if(disposing)
        {
            resendQueue.Dispose();
        }
        disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
