using System.Net.Mqtt.Server.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.Hosting;

public interface IMqttHostBuilder
{
    IMqttHostBuilder ConfigureAppConfiguration(Action<MqttHostBuilderContext, IConfigurationBuilder> configure);
    IMqttHostBuilder ConfigureServices(Action<MqttHostBuilderContext, IServiceCollection> configure);
    IMqttHostBuilder ConfigureOptions(Action<MqttHostBuilderContext, MqttServerBuilderOptions> configureOptions);
}