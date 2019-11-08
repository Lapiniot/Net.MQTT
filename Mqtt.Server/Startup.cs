using System.Diagnostics.CodeAnalysis;
using System.Net.Mqtt.Server.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mqtt.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWebSocketListener(Configuration.GetSection("WSListener"))
                .AddHealthChecks();
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static",
            Justification = "Method is used by infrastructure as a part of convention")]
        public void Configure(IApplicationBuilder app)
        {
            app
                .UseWebSockets()
                .UseWebSocketListener(new PathString("/mqtt"))
                .UseHealthChecks(new PathString("/health"));
        }
    }
}