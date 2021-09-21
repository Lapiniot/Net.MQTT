using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace System.Net.Mqtt.Server.Hosting;

public class MqttHostBuilderContext
{
    public IHostEnvironment HostingEnvironment { get; set; }

    public IConfiguration Configuration { get; set; }
}