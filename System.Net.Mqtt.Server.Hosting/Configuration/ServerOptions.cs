﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net.Connections;

namespace System.Net.Mqtt.Server.Hosting.Configuration;

public sealed class ServerOptions : MqttOptions
{
    [MinLength(1)]
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public Dictionary<string, Func<IAsyncEnumerable<NetworkConnection>>> ListenerFactories { get; } = new();

    [Range(1, int.MaxValue)]
    public int ConnectTimeout { get; set; }

    public ProtocolLevel ProtocolLevel { get; set; }

    public MqttOptions5 MQTT5 { get; set; }
}

public sealed class MqttOptions5 : MqttOptions { }

public abstract class MqttOptions
{
    [Range(1, ushort.MaxValue)]
    public ushort? MaxInFlight { get; set; }

    [Range(1, ushort.MaxValue)]
    public ushort? MaxReceive { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxUnflushedBytes { get; set; }

    [Range(128, int.MaxValue)]
    public int? MaxPacketSize { get; set; }
}

[Flags]
public enum ProtocolLevel
{
#pragma warning disable CA1707
    Mqtt3_1 = 0b01,
    Mqtt3_1_1 = 0b10,
    Mqtt5 = 0b100,
    Level3 = Mqtt3_1,
    Level4 = Mqtt3_1_1,
    Level5 = Mqtt5,
    Mqtt3 = Mqtt3_1 | Mqtt3_1_1,
    All = Mqtt3_1 | Mqtt3_1_1 | Mqtt5
}