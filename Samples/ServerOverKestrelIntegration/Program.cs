using Net.Mqtt.Server.AspNetCore.Hosting;
using Net.Mqtt.Server.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add the MQTT server to the service collection
builder.Services.AddMqttServer();

builder.Services.AddConnections();

builder.WebHost.ConfigureKestrel(options =>
    {
        // Listen for incoming connections on port 1883 and forward them to the MQTT server for processing.
        options.ListenAnyIP(1883, listenOptions => listenOptions.UseMqttServer());
        // Listen for incoming connections on port 8083. This is used for MQTT over WebSockets.
        options.ListenAnyIP(8083);
    });

// Enable MQTT server integration with Kestrel
builder.WebHost.UseMqtt();

// Configure the MQTT server to use HTTP server integration
builder.Host.ConfigureMqttServer((ctx, options) =>
{
    options.UseHttpServer();
    options.UseHttpServerWebSocketConnections();
});

var app = builder.Build();

// Map the MQTT over WebSockets endpoint (/mqtt by default)
app.MapMqttWebSockets();

app.Run();