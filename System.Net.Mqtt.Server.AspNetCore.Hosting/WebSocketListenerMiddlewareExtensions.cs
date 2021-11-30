﻿using System.Net.Mqtt.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting;

public static class WebSocketListenerMiddlewareExtensions
{
    public static IApplicationBuilder MapWebSocketInterceptor(this IApplicationBuilder builder, PathString pathMatch)
    {
        return builder.Map(pathMatch, builder => builder.UseMiddleware<WebSocketInterceptorMiddleware>());
    }

    public static IMqttHostBuilder UseWebSocketInterceptor(this IMqttHostBuilder builder)
    {
        return UseWebSocketInterceptor(builder, _ => { });
    }

    public static IMqttHostBuilder UseWebSocketInterceptor(this IMqttHostBuilder builder, Action<WebSocketListenerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices((ctx, services) => services
            .Configure<WebSocketListenerOptions>(ctx.Configuration.GetSection("WSListener"))
            .PostConfigure(configureOptions)
            .AddSingleton<HttpServerWebSocketAdapter>()
            .AddTransient<IAcceptedWebSocketHandler>(ResolveAdapterService));

        builder.ConfigureOptions((ctx, options) => options.ListenerFactories.Add("aspnet.websockets", ResolveAdapterService));

        return builder;
    }

    private static HttpServerWebSocketAdapter ResolveAdapterService(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<HttpServerWebSocketAdapter>();
    }
}