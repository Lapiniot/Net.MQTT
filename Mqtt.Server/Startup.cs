using System.Net.Mqtt.Server.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mqtt.Server
{
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWebSocketListener()
                .AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
        {
            app
                .UseWebSockets()
                .UseWebSocketListener()
                .UseHealthChecks(new PathString("/health"));
        }
    }
}