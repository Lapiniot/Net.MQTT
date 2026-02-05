using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

#pragma warning disable CA1034 // Nested types should not be visible

namespace Net.Mqtt.Server.Hosting;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public bool TryGetSwitch(string switchName, out bool isEnabled)
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
}