using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.Hosting
{
    internal class MqttHostBuilder : IMqttHostBuilder
    {
        private const string RootSectionName = "MQTT";

        private IHostBuilder hostBuilder;
        private MqttHostBuilderContext context;
        private MqttServerBuilderOptions options;

        public MqttHostBuilder(IHostBuilder hostBuilder)
        {
            this.hostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

            options = new MqttServerBuilderOptions();

            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables($"{RootSectionName}_");

            hostBuilder.ConfigureHostConfiguration(cb =>
            {
                cb.AddConfiguration(configBuilder.Build());
            });

            hostBuilder.ConfigureServices((ctx, services) =>
            {
                BuildOptions(ctx.Configuration, ctx.HostingEnvironment);

                services.AddHostedService(sp => new GenericMqttHostService(
                    (new MqttServerBuilder(options, sp,
                        sp.GetRequiredService<ILoggerFactory>(),
                        sp.GetService<IMqttAuthenticationHandler>())),
                    sp.GetRequiredService<ILogger<GenericMqttHostService>>()));
            });
        }

        public IMqttHostBuilder ConfigureAppConfiguration(Action<MqttHostBuilderContext, IConfigurationBuilder> configure)
        {
            hostBuilder.ConfigureAppConfiguration((ctx, configBuilder) =>
            {
                configure(GetContext(ctx), configBuilder);
            });

            return this;
        }

        public IMqttHostBuilder ConfigureServices(Action<MqttHostBuilderContext, IServiceCollection> configure)
        {
            hostBuilder.ConfigureServices((ctx, services) =>
            {
                configure(GetContext(ctx), services);
            });

            return this;
        }

        public IMqttHostBuilder ConfigureOptions(Action<MqttServerBuilderOptions> configureOptions)
        {
            if(configureOptions is null) throw new ArgumentNullException(nameof(configureOptions));

            hostBuilder.ConfigureServices((ctx, services) =>
            {
                BuildOptions(ctx.Configuration, ctx.HostingEnvironment);
                configureOptions(options);
            });

            return this;
        }

        private MqttHostBuilderContext GetContext(HostBuilderContext hostBuilderContext)
        {
            return context ??= new MqttHostBuilderContext()
            {
                HostingEnvironment = hostBuilderContext.HostingEnvironment,
                Configuration = hostBuilderContext.Configuration
            };
        }

        private void BuildOptions(IConfiguration configuration, IHostEnvironment environment)
        {
            var section = configuration.GetSection(RootSectionName);
            var endpoints = section.GetSection("Endpoints");
            var certificates = section.GetSection("Certificates");

            section.Bind(options);

            foreach(var config in endpoints.GetChildren())
            {
                if(!options.ListenerFactories.ContainsKey(config.Key))
                {
                    var certificateSection = config.GetSection("Certificate");

                    if(certificateSection.Exists())
                    {
                        var certOptions = ResolveCertificateOptions(certificateSection, certificates);

                        if(certOptions.Path is not null)
                        {
                            var path = environment.ContentRootFileProvider.GetFileInfo(certOptions.Path).PhysicalPath;
                            var keyPath = environment.ContentRootFileProvider.GetFileInfo(certOptions.KeyPath).PhysicalPath;
                            var password = certOptions.Password;

                            options.UseSslEndpoint(config.Key, new Uri(config.Value ?? config.GetValue<string>("Url")),
                                () => CertificateLoader.LoadFromFile(path, keyPath, password));
                        }
                        else if(certOptions.Subject is not null)
                        {
                            var store = certOptions.Store;
                            var location = certOptions.Location;
                            var subject = certOptions.Subject;
                            var allowInvalid = certOptions.AllowInvalid;

                            options.UseSslEndpoint(config.Key, new Uri(config.Value ?? config.GetValue<string>("Url")),
                                () => CertificateLoader.LoadFromStore(store, location, subject, allowInvalid));
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot load certificate from configuration, either store information or file path should be provided");
                        }
                    }
                    else
                    {
                        options.UseEndpoint(config.Key, new Uri(config.Value ?? config.GetValue<string>("Url")));
                    }
                }
            }
        }

        private static CertificateOptions ResolveCertificateOptions(IConfigurationSection certificate, IConfigurationSection certificates)
        {
            if(certificate.Value is not null)
            {
                var cert = certificates.GetSection(certificate.Value);

                return cert.Exists()
                    ? cert.Get<CertificateOptions>()
                    : throw new InvalidOperationException("Certificate configuration '" + certificate.Value + "' is missing");
            }
            else
            {
                return certificate.Get<CertificateOptions>();
            }
        }
    }
}