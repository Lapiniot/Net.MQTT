using System.Runtime.CompilerServices;

namespace System.Net.Mqtt.Server.Protocol.V4;

public sealed class MqttServerSessionState : V3.MqttServerSessionState
{
    public MqttServerSessionState(string clientId, DateTime createdAt) :
        base(clientId, createdAt)
    { }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected override byte AddFilter(string filter, byte qosLevel)
    {
        return TryAdd(filter, qosLevel) ? qosLevel : (byte)0x80;
    }
}