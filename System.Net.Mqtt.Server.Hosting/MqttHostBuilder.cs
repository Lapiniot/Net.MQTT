using System.Net.Mqtt.Server.Hosting.Configuration;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting;

internal class MqttHostBuilder : IMqttHostBuilder
{
    private const string RootSectionName = "MQTT";

    private readonly IHostBuilder hostBuilder;
    private readonly MqttServerBuilderOptions options;
    private MqttHostBuilderContext context;

    public MqttHostBuilder(IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        this.hostBuilder = hostBuilder;

        options = new();

        var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables($"{RootSectionName}_");

        hostBuilder.ConfigureHostConfiguration(cb => cb.AddConfiguration(configBuilder.Build()));

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            ApplyConfiguration(ctx.Configuration, ctx.HostingEnvironment);

            services.AddHostedService(sp => new GenericMqttHostService(
                new MqttServerBuilder(options, sp,
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetService<IMqttAuthenticationHandler>()),
                sp.GetRequiredService<IHostApplicationLifetime>(),
                sp.GetRequiredService<ILogger<GenericMqttHostService>>()));
        });
    }

    private MqttHostBuilderContext GetContext(HostBuilderContext hostBuilderContext) =>
        context ??= new()
        {
            HostingEnvironment = hostBuilderContext.HostingEnvironment,
            Configuration = hostBuilderContext.Configuration
        };

    private void ApplyConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(RootSectionName);
        var endpoints = section.GetSection("Endpoints");
        var certificates = section.GetSection("Certificates");

        section.Bind(options);

        foreach (var config in endpoints.GetChildren())
        {
            var certificateSection = config.GetSection("Certificate");

            if (certificateSection.Exists())
            {
                var protocols = ResolveSslProtocolsOptions(config.GetSection("SslProtocols"));

                var certOptions = ResolveCertificateOptions(certificateSection, certificates);

                if (certOptions.Path is not null)
                {
                    var path = environment.ContentRootFileProvider.GetFileInfo(certOptions.Path).PhysicalPath;
                    var keyPath = environment.ContentRootFileProvider.GetFileInfo(certOptions.KeyPath).PhysicalPath;
                    var password = certOptions.Password;

                    options.UseSslEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")),
                        () => CertificateLoader.LoadFromFile(path, keyPath, password), protocols,
                        config.GetValue("ClientCertificateMode", ClientCertificateMode.NoCertificate));
                }
                else if (certOptions.Subject is not null)
                {
                    var store = certOptions.Store;
                    var location = certOptions.Location;
                    var subject = certOptions.Subject;
                    var allowInvalid = certOptions.AllowInvalid;

                    options.UseSslEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")),
                        () => CertificateLoader.LoadFromStore(store, location, subject, allowInvalid), protocols,
                        config.GetValue("ClientCertificateMode", ClientCertificateMode.NoCertificate));
                }
                else
                {
                    throw new InvalidOperationException("Cannot load certificate from configuration, either store information or file path should be provided");
                }
            }
            else
            {
                options.UseEndpoint(config.Key, new(config.Value ?? config.GetValue<string>("Url")));
            }
        }
    }

    private static SslProtocols ResolveSslProtocolsOptions(IConfigurationSection configuration)
    {
        var protocols = SslProtocols.None;

        if (!configuration.Exists()) return protocols;

        if (configuration.Value is not null)
        {
            protocols = Enum.Parse<SslProtocols>(configuration.Value);
        }
        else
        {
            foreach (var p in configuration.GetChildren())
            {
                protocols |= Enum.Parse<SslProtocols>(p.Value);
            }
        }

        return protocols;
    }

    private static CertificateOptions ResolveCertificateOptions(IConfigurationSection certificate, IConfiguration certificates)
    {
        var certName = certificate.Value;

        if (certName is null) return certificate.Get<CertificateOptions>();

        var certSection = certificates.GetSection(certName);

        return certSection.Exists()
            ? certSection.Get<CertificateOptions>()
            : throw new InvalidOperationException($"Certificate configuration for '{certName}' is missing");
    }

    public IMqttHostBuilder ConfigureAppConfiguration(Action<MqttHostBuilderContext, IConfigurationBuilder> configure)
    {
        hostBuilder.ConfigureAppConfiguration((ctx, configBuilder) => configure(GetContext(ctx), configBuilder));

        return this;
    }

    public IMqttHostBuilder ConfigureServices(Action<MqttHostBuilderContext, IServiceCollection> configure)
    {
        hostBuilder.ConfigureServices((ctx, services) => configure(GetContext(ctx), services));

        return this;
    }

    public IMqttHostBuilder ConfigureOptions(Action<MqttHostBuilderContext, MqttServerBuilderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        hostBuilder.ConfigureServices((ctx, _) => configureOptions(GetContext(ctx), options));

        return this;
    }
}