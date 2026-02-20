using Microsoft.AspNetCore.Identity;
using Mqtt.Server.Identity;
using Mqtt.Server.Identity.Data;
using Mqtt.Server.Identity.Stores;

var builder = Host.CreateApplicationBuilder(args);

var identity = builder.Services
    .Configure<IdentityOptions>(options =>
    {
#if NET10_0_OR_GREATER
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
#else
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
#endif
    })
    .AddDbContext<ApplicationDbContext>(options =>
        options.ConfigureProvider(builder.Configuration));

var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);

await InitializeIdentityExtensions.InitializeIdentityStoreAsync(host.Services).ConfigureAwait(false);

await host.StopAsync().ConfigureAwait(false);