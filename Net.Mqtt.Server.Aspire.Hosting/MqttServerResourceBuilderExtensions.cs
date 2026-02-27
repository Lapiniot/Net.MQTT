using System.Net.Sockets;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1708 // Identifiers should differ by more than case
#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Net.Mqtt.Server.Aspire.Hosting;

/// <summary>
/// Extension methods for adding and configuring MQTT server resources in an Aspire.Hosting application.
/// This includes methods for adding an MQTT server resource with common configurations, as well as methods
/// for adding specific endpoints (TCP, TCP with SSL, Kestrel-based).
/// </summary>
public static class MqttServerResourceBuilderExtensions
{
    private const string Image = "lapiniot/mqtt-server";
    private const string Registry = "docker.io";
    private const string Tag = "latest";
    private const string TcpEndpointName = "mqtt";
    private const string TcpSslEndpointName = "mqtts";
    private const string HttpEndpointName = "http";
    private const string HttpsEndpointName = "https";
    private const int DefaultTcpPort = 1883;
    private const int DefaultTcpSslPort = 8883;
    private const int DefaultHttpPort = 8001;
    private const int DefaultHttpsPort = 8002;
    private const string KestrelCertPathVarName = "Kestrel__Certificates__Default__Path";
    private const string KestrelCertKeyPathVarName = "Kestrel__Certificates__Default__KeyPath";
    private const string KestrelCertPasswordVarName = "Kestrel__Certificates__Default__Password";
    private const string MqttCertPathVarName = "MQTT__Certificates__Default__Path";
    private const string MqttCertKeyPathVarName = "MQTT__Certificates__Default__KeyPath";
    private const string MqttCertPasswordVarName = "MQTT__Certificates__Default__Password";

    extension(IDistributedApplicationBuilder builder)
    {
        /// <summary>
        /// Adds an MQTT server resource to the application. The resource will be configured with a TCP endpoint
        /// for MQTT communication and an HTTP endpoint for the admin UI. The HTTP endpoint will also be used
        /// to serve the MQTT over WebSockets endpoint at the path "/mqtt". If HTTPS is configured, it will also
        /// serve MQTT over Secure WebSockets at the same path.
        /// </summary>
        /// <param name="name">The name of the MQTT server resource.</param>
        /// <returns>The resource builder for the MQTT server resource.</returns>
        public IResourceBuilder<MqttServerResource> AddMqttServer(string name)
        {
            var resource = new MqttServerResource(name);

            var resourceBuilder = builder.AddResource(resource)
                .WithImage(Image)
                .WithImageRegistry(Registry)
                .WithImageTag(Tag)
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
                .WithTcpEndpoint()
                .WithHttpEndpointDefaults();

            return resourceBuilder;
        }
    }

    extension<T>(IResourceBuilder<T> resourceBuilder) where T :
        IResourceWithEndpoints,
        IResourceWithEnvironment,
        IResourceWithArgs
    {
        /// <summary>
        /// Adds a TCP endpoint for MQTT communication. The endpoint will use the "mqtt" URI scheme and the "mqtt" transport.
        /// </summary>
        /// <param name="port">The port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="targetPort">The target port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="isProxied">Whether the endpoint is proxied. If false, the endpoint is not proxied.</param>
        /// <param name="isExternal">Whether the endpoint is external. If false, the endpoint is internal.</param>
        /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
        public IResourceBuilder<T> WithTcpEndpoint(int? port = null,
            int? targetPort = null, bool isProxied = true, bool isExternal = true)
        {
            return resourceBuilder
                .WithEndpoint(TcpEndpointName, ep =>
                {
                    ep.Protocol = ProtocolType.Tcp;
                    ep.UriScheme = "mqtt";
                    ep.Transport = "mqtt";
                    ep.Port = port ?? DefaultTcpPort;
                    ep.TargetPort = targetPort ?? (resourceBuilder.RequiresTargetPort() ? DefaultTcpPort : null);
                    ep.IsProxied = isProxied;
                    ep.IsExternal = isExternal;
                })
                .WithEnvironment(ctx =>
                {
                    var resource = (IResourceWithEndpoints)ctx.Resource;
                    var tcpEndpoint = resource.GetEndpoint(TcpEndpointName);
                    ctx.EnvironmentVariables[$"MQTT__Endpoints__{TcpEndpointName}__Port"] =
                        tcpEndpoint.Property(EndpointProperty.TargetPort);
                })
                .WithUrlForEndpoint(TcpEndpointName, url => url.DisplayText = $"{url.Url} (TCP)");
        }

        /// <summary>
        /// Adds a TCP endpoint with SSL for MQTT communication. The endpoint will use the "mqtts" URI scheme and the "mqtts" transport.
        /// </summary>
        /// <param name="port">The port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="targetPort">The target port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="isProxied">Whether the endpoint is proxied. If false, the endpoint is not proxied.</param>
        /// <param name="isExternal">Whether the endpoint is external. If false, the endpoint is internal.</param>
        /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
        public IResourceBuilder<T> WithTcpSslEndpoint(int? port = null,
            int? targetPort = null, bool isProxied = true, bool isExternal = true)
        {
            return resourceBuilder
                .WithEndpoint(TcpSslEndpointName, ep =>
                {
                    ep.Protocol = ProtocolType.Tcp;
                    ep.UriScheme = "mqtts";
                    ep.Transport = "mqtts";
                    ep.Port = port ?? DefaultTcpSslPort;
                    ep.TargetPort = targetPort ?? (resourceBuilder.RequiresTargetPort() ? DefaultTcpSslPort : null);
                    ep.IsProxied = isProxied;
                    ep.IsExternal = isExternal;
                })
                .WithEnvironment(ctx =>
                {
                    var resource = (IResourceWithEndpoints)ctx.Resource;
                    var tcpEndpoint = resource.GetEndpoint(TcpSslEndpointName);
                    ctx.EnvironmentVariables[$"MQTT__Endpoints__{TcpSslEndpointName}__Port"] =
                        tcpEndpoint.Property(EndpointProperty.TargetPort);
                })
                .WithUrlForEndpoint(TcpSslEndpointName, url => url.DisplayText = $"{url.Url} (TCP.SSL)")
                .WithMqttDefaultCertificateConfiguration();
        }

        /// <summary>
        /// Adds a TCP endpoint for MQTT communication that is exposed via Kestrel. The endpoint will use the "mqtt" URI scheme and the "mqtt" transport.
        /// </summary>
        /// <param name="port">The port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="targetPort">The target port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="isProxied">Whether the endpoint is proxied. If false, the endpoint is not proxied.</param>
        /// <param name="isExternal">Whether the endpoint is external. If false, the endpoint is internal.</param>
        /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
        public IResourceBuilder<T> WithKestrelTcpEndpoint(int? port = null,
            int? targetPort = null, bool isProxied = true, bool isExternal = true)
        {
            const string EndpointName = $"{TcpEndpointName}-kestrel";

            return resourceBuilder
                .WithEndpoint(EndpointName, annotation =>
                {
                    annotation.Protocol = ProtocolType.Tcp;
                    annotation.Transport = "mqtt";
                    annotation.UriScheme = "mqtt";
                    annotation.Port = port ?? 1884;
                    annotation.TargetPort = targetPort ?? (resourceBuilder.RequiresTargetPort() ? 1884 : null);
                    annotation.IsProxied = isProxied;
                    annotation.IsExternal = isExternal;
                })
                .WithEnvironment(static context =>
                {
                    var resource = (IResourceWithEndpoints)context.Resource;
                    if (resource.GetEndpoint(EndpointName) is { } endpoint)
                    {
                        context.EnvironmentVariables[$"Kestrel__Endpoints__{EndpointName}__Url"] =
                            ReferenceExpression.Create($"http://*:{endpoint.Property(EndpointProperty.TargetPort)}");
                        context.EnvironmentVariables[$"Kestrel__Endpoints__{EndpointName}__Protocols"] = "Http1";
                        context.EnvironmentVariables[$"Kestrel__Endpoints__{EndpointName}__UseMqtt"] = true;
                    }
                })
                .WithUrlForEndpoint(EndpointName, url => url.DisplayText = $"{url.Url} (TCP via Kestrel)");
        }

        /// <summary>
        /// Adds a TCP endpoint with SSL for MQTT communication that is exposed via Kestrel. The endpoint will use the "mqtts" URI scheme and the "mqtts" transport.
        /// </summary>
        /// <param name="port">The port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="targetPort">The target port number to use for the endpoint. If null, the default port is used.</param>
        /// <param name="isProxied">Whether the endpoint is proxied. If false, the endpoint is not proxied.</param>
        /// <param name="isExternal">Whether the endpoint is external. If false, the endpoint is internal.</param>
        /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
        public IResourceBuilder<T> WithKestrelTcpSslEndpoint(int? port = null,
            int? targetPort = null, bool isProxied = true, bool isExternal = true)
        {
            const string EndpointName = $"{TcpSslEndpointName}-kestrel";

            return resourceBuilder
                .WithEndpoint(EndpointName, annotation =>
                {
                    annotation.Protocol = ProtocolType.Tcp;
                    annotation.Transport = "mqtts";
                    annotation.UriScheme = "mqtts";
                    annotation.Port = port ?? 8884;
                    annotation.TargetPort = targetPort ?? (resourceBuilder.RequiresTargetPort() ? 8884 : null);
                    annotation.IsProxied = isProxied;
                    annotation.IsExternal = isExternal;
                })
                .WithEnvironment(static context =>
                {
                    var resource = (IResourceWithEndpoints)context.Resource;
                    if (resource.GetEndpoint(EndpointName) is { } endpoint)
                    {
                        context.EnvironmentVariables[$"Kestrel__Endpoints__{EndpointName}__Url"] =
                            ReferenceExpression.Create($"https://*:{endpoint.Property(EndpointProperty.TargetPort)}");
                        context.EnvironmentVariables[$"Kestrel__Endpoints__{EndpointName}__Protocols"] = "Http1";
                        context.EnvironmentVariables[$"Kestrel__Endpoints__{EndpointName}__UseMqtt"] = true;
                    }
                })
                .WithUrlForEndpoint(EndpointName, url => url.DisplayText = $"{url.Url} (TCP.SSL via Kestrel)")
                .WithKestrelDefaultCertificateConfiguration();
        }

        /// <summary>
        /// Adds an HTTP endpoint with relevant default configuration for the MQTT admin UI. 
        /// It will also be used to serve MQTT over Secure WebSockets at the path "/mqtt".
        /// </summary>
        /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
        public IResourceBuilder<T> WithHttpEndpointDefaults()
        {
            return resourceBuilder
                .WithEndpoint(HttpEndpointName, endpoint =>
                {
                    endpoint.Port = DefaultHttpPort;
                    endpoint.TargetPort = resourceBuilder.RequiresTargetPort() ? DefaultHttpPort : null;
                    endpoint.UriScheme = "http";
                    endpoint.Protocol = ProtocolType.Tcp;
                    endpoint.IsExternal = true;
                    endpoint.IsProxied = true;
                })
                .WithEnvironment(static context =>
                {
                    var resource = (IResourceWithEndpoints)context.Resource;
                    var endpoint = resource.GetEndpoint(HttpEndpointName);
                    context.EnvironmentVariables[$"Kestrel__Endpoints__{HttpEndpointName}__Url"] =
                        ReferenceExpression.Create($"http://*:{endpoint.Property(EndpointProperty.TargetPort)}");
                })
                .WithUrlForEndpoint(HttpEndpointName, static url => url.DisplayText = $"{url.Url} (MQTT Admin UI)")
                .WithUrlForEndpoint(HttpEndpointName, static ep =>
                {
                    var url = new UriBuilder(ep.Url) { Scheme = "ws", Path = "/mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                    return new ResourceUrlAnnotation
                    {
                        Endpoint = ep,
                        Url = url,
                        DisplayText = $"{url} (WebSockets)"
                    };
                });
        }

        /// <summary>
        /// Adds an HTTPS endpoint with relevant default configuration for the MQTT admin UI. 
        /// It will also be used to serve MQTT over Secure WebSockets at the path "/mqtt".
        /// </summary>
        /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
        public IResourceBuilder<T> WithHttpsEndpointDefaults()
        {
            return resourceBuilder
                .WithEndpoint(HttpsEndpointName, endpoint =>
                {
                    endpoint.Port = DefaultHttpsPort;
                    endpoint.TargetPort = resourceBuilder.RequiresTargetPort() ? DefaultHttpsPort : null;
                    endpoint.UriScheme = "https";
                    endpoint.Protocol = ProtocolType.Tcp;
                    endpoint.IsExternal = true;
                    endpoint.IsProxied = true;
                })
                .WithEnvironment(static context =>
                {
                    var resource = (IResourceWithEndpoints)context.Resource;
                    var endpoint = resource.GetEndpoint(HttpsEndpointName);
                    context.EnvironmentVariables[$"Kestrel__Endpoints__{HttpsEndpointName}__Url"] =
                        ReferenceExpression.Create($"https://*:{endpoint.Property(EndpointProperty.TargetPort)}");
                })
                .WithUrlForEndpoint(HttpsEndpointName, static url => url.DisplayText = $"{url.Url} (MQTT Admin UI)")
                .WithUrlForEndpoint(HttpsEndpointName, static ep =>
                {
                    var url = new UriBuilder(ep.Url) { Scheme = "wss", Path = "/mqtt" }.Uri.AbsoluteUri.TrimEnd('/');
                    return new ResourceUrlAnnotation
                    {
                        Endpoint = ep,
                        Url = url,
                        DisplayText = $"{url} (Secure WebSockets)"
                    };
                })
                .WithKestrelDefaultCertificateConfiguration();
        }

        public IResourceBuilder<T> WithKestrelDefaultCertificateConfiguration()
        {
            return resourceBuilder.WithHttpsCertificateConfiguration(static ctx =>
                {
                    ctx.EnvironmentVariables[KestrelCertPathVarName] = ctx.CertificatePath;
                    ctx.EnvironmentVariables[KestrelCertKeyPathVarName] = ctx.KeyPath;
                    if (ctx.Password is not null)
                    {
                        ctx.EnvironmentVariables[KestrelCertPasswordVarName] = ctx.Password;
                    }

                    return Task.CompletedTask;
                });
        }

        public IResourceBuilder<T> WithMqttDefaultCertificateConfiguration()
        {
            return resourceBuilder.WithHttpsCertificateConfiguration(static ctx =>
                {
                    ctx.EnvironmentVariables[MqttCertPathVarName] = ctx.CertificatePath;
                    ctx.EnvironmentVariables[MqttCertKeyPathVarName] = ctx.KeyPath;
                    if (ctx.Password is not null)
                    {
                        ctx.EnvironmentVariables[MqttCertPasswordVarName] = ctx.Password;
                    }

                    return Task.CompletedTask;
                });
        }

        public IResourceBuilder<T> WithKestrelDefaultCertificate(
            ReferenceExpression certificatePath,
            ReferenceExpression? certificateKeyPath = null,
            ReferenceExpression? certificatePassword = null)
        {
            resourceBuilder.WithEnvironment(KestrelCertPathVarName, certificatePath);

            if (certificateKeyPath is not null)
            {
                resourceBuilder.WithEnvironment(KestrelCertKeyPathVarName, certificateKeyPath);
            }

            if (certificatePassword is not null)
            {
                resourceBuilder.WithEnvironment(KestrelCertPasswordVarName, certificatePassword);
            }

            return resourceBuilder;
        }

        public IResourceBuilder<T> WithMqttDefaultCertificate(
            ReferenceExpression certificatePath,
            ReferenceExpression? certificateKeyPath = null,
            ReferenceExpression? certificatePassword = null)
        {
            resourceBuilder.WithEnvironment(MqttCertPathVarName, certificatePath);

            if (certificateKeyPath is not null)
            {
                resourceBuilder.WithEnvironment(MqttCertKeyPathVarName, certificateKeyPath);
            }

            if (certificatePassword is not null)
            {
                resourceBuilder.WithEnvironment(MqttCertPasswordVarName, certificatePassword);
            }

            return resourceBuilder;
        }

        public IResourceBuilder<T> PublishWithSecureEndpoints(Action<IResourceBuilder<T>> configure)
        {
            return resourceBuilder.ApplicationBuilder.ExecutionContext.IsPublishMode
                ? resourceBuilder.WithSecureEndpoints(configure)
                : resourceBuilder;
        }

        public IResourceBuilder<T> RunWithSecureEndpoints(Action<IResourceBuilder<T>> configure)
        {
            return resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode
                ? resourceBuilder.WithSecureEndpoints(configure)
                : resourceBuilder;
        }

        public IResourceBuilder<T> WithSecureEndpoints(Action<IResourceBuilder<T>> configure)
        {
            resourceBuilder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
            {
                var developerCertificateService = @event.Services.GetRequiredService<IDeveloperCertificateService>();

                var addHttps = resourceBuilder.Resource.TryGetLastAnnotation<HttpsCertificateAnnotation>(out var annotation)
                    ? annotation.UseDeveloperCertificate.GetValueOrDefault(developerCertificateService.UseForHttps)
                        || annotation.Certificate is not null
                    : developerCertificateService.UseForHttps;

                if (addHttps)
                {
                    configure(resourceBuilder);
                }

                return Task.CompletedTask;
            });

            return resourceBuilder;
        }

        private bool RequiresTargetPort() => resourceBuilder.Resource.IsContainer()
            || resourceBuilder.ApplicationBuilder.ExecutionContext.IsPublishMode
                && resourceBuilder.Resource.RequiresImageBuild();
    }

    extension(IResourceBuilder<MqttServerResource> resourceBuilder)
    {
        /// <summary>
        /// Adds a data volume to the MQTT server resource. The volume will be mounted at "/home/app" in the container.
        /// </summary>
        /// <param name="name">The name of the volume. If null, a name will be generated based on the resource name.</param>
        /// <param name="isReadOnly">Whether the volume is read-only. If true, the volume will be mounted as read-only.</param>
        /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
        public IResourceBuilder<MqttServerResource> WithDataVolume(string? name = null, bool isReadOnly = false)
        {
            return resourceBuilder.WithVolume(name ?? VolumeNameGenerator.Generate(resourceBuilder, "data"),
                "/home/app", isReadOnly);
        }
    }
}