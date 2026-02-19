## General

Net.MQTT is a modular, high-performance [MQTT (Message Queuing Telemetry Transport)](https://mqtt.org) server and client framework written in .NET.
It is designed for building scalable and performant messaging solutions that can be easily integrated into a wide range of applications and services. The framework is built on top of the latest .NET technologies and follows modern design principles to provide a flexible and extensible architecture for MQTT communication. Both MQTT 3.1.1 and MQTT 5.0 protocol versions are supported, clients of different protocol versions can connect to the same server and interact with each other seamlessly.

The project provides:

- A robust, highly configurable MQTT broker/server implementation
- Server libraries can be used separately to build custom server implementations tailored to specific requirements
- Flexible client libraries for .NET applications
- MQTT benchmarking tool for performance and load testing
- Support for containerized deployments and cloud-native scenarios

Net.MQTT supports multiple network transports for MQTT communication:

- **TCP** (including TCP over SSL/TLS for secure connections)
- **Unix Domain Sockets** (for efficient local inter-process communication)
- **WebSockets** (over HTTP/1.1 and HTTP/2, for browser and web integration)
- **QUIC** (experimental, for low-latency, multiplexed UDP-based connections)

In order to build a custom, fully functional MQTT server, you can choose from a variety of hosting models and integration options:
- **Generic Host** - build and run MQTT server as a part of the .NET Generic Host application, which is ideal for background services and non-HTTP workloads
- **ASP.NET Core hosting** - build MQTT server into ASP.NET Core application and leverage ASP.NET Core's hosting, configuration, and dependency injection features, which is ideal for web applications and services that need to expose MQTT functionality alongside HTTP endpoints. Kestrel server connections pipeline may be used as a transport for MQTT over TCP and WebSockets
- **Custom hosting** - build MQTT server with a custom hosting model as a part of any .NET application, using the MQTT server libraries directly without relying on ASP.NET Core or Generic Host

## Getting started

To get started with Net.MQTT, please refer to the documentation and samples provided in the repository:
- [Mqtt.Server](Mqtt.Server) - a fully functional MQTT server implementation with web admin UI, telemetry and metrics support, health checks 
 and various configuration options. This project can be used as a ready-to-use MQTT broker or as a reference implementation for building custom MQTT servers using the server libraries
- [Samples/Client](Samples/Client) - a sample MQTT client application demonstrating how to connect, publish, and subscribe to topics using the Net.MQTT client library
- [Samples/Server](Samples/Server) - a sample MQTT server application demonstrating how to build a custom MQTT server using the Net.MQTT server libraries without relying on ASP.NET Core or Generic Host
- [Samples/ServerAsHostedBackgroundService](Samples/ServerAsHostedBackgroundService) - a sample MQTT server application demonstrating how to build a custom MQTT server as a .NET Generic Host background service
- [Samples/ServerOverKestrelIntegration](Samples/ServerOverKestrelIntegration) - a sample MQTT server application demonstrating how to build a custom MQTT server integrated with ASP.NET Core and Kestrel server
- [Mqtt.Server.AppHost](Mqtt.Server.AppHost) - an Aspire app for running the MQTT server with cloud-native features, including distributed tracing, health checks, and service discovery. This project demonstrates how to integrate Net.MQTT with the .NET Aspire orchestration platform for modern cloud scenarios.

## Containers

### How to build container images:

Build container image for Linux and x64 architecture. Distroless 'Ubuntu Chiseled' base image is used by default.
Image is pushed to the local docker image repository automatically.

``` sh
dotnet publish /t:PublishContainer -c Release -f net10.0 -r linux-x64 --self-contained /p:PublishTrimmed=true /p:SuppressTrimAnalysisWarnings=true ./Mqtt.Server
```
> [!TIP]
>
> **-r** switch denotes target container runtime identifier in the form *[OS name]-[architecture]* e.g.:
>
>* **-r linux-x64** - build Linux image for x64
>* **-r linux-arm64** - build Linux image for ARM64
>* **-r linux-musl-x64** - build Linux image for x64, but use slim 'musl libc' based Alpine distro as base image


Build container image with name 'docker_user/mqtt-server' and publish to the online registry (docker.io e.g.)

``` sh
dotnet publish /t:PublishContainer -c Release -f net10.0 -r linux-x64 --self-contained /p:PublishTrimmed=true /p:SuppressTrimAnalysisWarnings=true /p:ContainerRepository='docker_user/mqtt-server' /p:ContainerRegistry='docker.io' ./Mqtt.Server
```

It is also possible to build and publish multi-architecture images:

``` sh
dotnet build /t:PublishContainer -c Release -f net10.0 \
/p:RuntimeIdentifiers='"linux-x64;linux-arm64"' \
/p:ContainerRepository='docker_user/mqtt-server' \
/p:ContainerRegistry='docker.io' \
/p:SelfContained=true \
/p:PublishTrimmed=true \
/p:SuppressTrimAnalysisWarnings=true \
./Mqtt.Server
```

### Run container from image:

``` sh
docker run -p 1883:1883 -p 8001:8001 -p 8002:8002 mqtt-server:latest
```

> [!NOTE]
> This command will run container image in a minimal default configuration:
> MQTT server will accept connections for:
> * hostname and port 1883 - MQTT TCP
> * http://hostname:8001/mqtt - HTTP WebSockets
> * http://hostname:8002/mqtt - HTTP WebSockets HTTP/2 only
>
> Web admin page will be also available at http://hostname:8001

If you need to persist application data and be able to customize default configuration, just bind container's **/home/app** directory to some persisted host's directory:
``` sh
docker run --volume=<some_host_directory_to_store_container_data>:/home/app -p 1883:1883 -p 8001:8001 -p 8002:8002 mqtt-server:latest
```

HTTPS and MQTT TCP SSL can be also configured additionally via environment variables:
``` sh
docker run --env=Kestrel__Endpoints__https__Url=https://*:8002 \
--env=Kestrel__Certificates__Default__Path=/home/app/.config/mqtt-server/mqtt-server.pfx \
--env=MQTT__Endpoints__tcp.ssl.default__Url=tcps://[::]:8883 \
--env=MQTT__Endpoints__tcp.ssl.default__Certificate__Path=/home/app/.config/mqtt-server/mqtt-server.pfx \
--volume=<some_host_directory_to_store_container_data>:/home/app \
-p 1883:1883 -p 8001:8001 -p 8002:8002 -p 8003:8003 -p 8883:8883 \
mqtt-server:latest
```

If you want to know how to generate self-signed SSL certificate on the app startup, please refer to:

[Example Docker Compose file](Mqtt.Server/docker-compose.yml)