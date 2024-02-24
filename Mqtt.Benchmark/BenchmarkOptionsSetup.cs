using Microsoft.Extensions.Configuration;

namespace Mqtt.Benchmark;

public class BenchmarkOptionsSetup(IConfiguration configuration) : IConfigureOptions<BenchmarkOptions>
{
    public void Configure([NotNull] BenchmarkOptions options)
    {
        var profiles = configuration.GetSection("Profiles");

        // First try to bind options as base ProfileOptions and read 
        // configuration defaults from "Profiles:Default" section
        ProfileOptions profile = options;
        profiles.GetSection("Default").Bind(profile);

        // If TestProfile parameter was configured, try to 
        // override options defaults from this profile
        if (configuration.GetValue<string>("TestProfile") is { } profileName)
        {
            var profileSection = profiles.GetSection(profileName);
            if (profileSection.Exists())
                profileSection.Bind(profile);
        }

        // Finally, read remaining parameters and override
        // previously configured parameters from root level 
        // section as these settings have highest priority 
        configuration.Bind(options);
    }
}