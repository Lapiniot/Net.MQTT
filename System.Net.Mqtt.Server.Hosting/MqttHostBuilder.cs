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
                BuildOptions(ctx.Configuration);

                services.AddHostedService(sp => new GenericMqttHostService(
                    (new MqttServerBuilder(options, sp,
                        sp.GetRequiredService<ILoggerFactory>(),
                        sp.GetService<IMqttAuthenticationHandler>())).Build(),
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
                BuildOptions(ctx.Configuration);
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

        private void BuildOptions(IConfiguration configuration)
        {
            var section = configuration.GetSection(RootSectionName);

            section.Bind(options);

            foreach(var item in section.GetSection("Endpoints").GetChildren())
            {
                if(!options.ListenerFactories.ContainsKey(item.Key))
                {
                    options.UseEndpoint(item.Key, new Uri(item.Value ?? item.GetValue<string>("Url")));
                }
            }
        }
    }
}