using Microsoft.Extensions.Hosting;
using Net.Mqtt.Server.Hosting;

var builder = Host.CreateApplicationBuilder(args: args);

builder.Services.AddMqttServer();

var host = builder.Build();

host.Run();