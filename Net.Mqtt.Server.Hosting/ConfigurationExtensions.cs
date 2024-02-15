using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Net.Mqtt.Server.Hosting;

public static class ConfigurationExtensions
{
    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public static bool TryGetSwitch(this IConfiguration configuration, string switchName, out bool isEnabled)
    {
        if (configuration.GetValue<bool?>(switchName) is { } value)
        {
            isEnabled = value;
            return true;
        }
        else
        {
            isEnabled = default;
            return false;
        }
    }
}