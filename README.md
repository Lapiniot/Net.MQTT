## General
## Getting started
## Containers

### How to build container images:

Build container image for Linux and x64 architecture. Distroless 'Ubuntu Chiseled' base image is used by default.
Image is pushed to the local docker image repository automatically.

```
dotnet publish /p:Configuration=Release /p:PublishProfile=DefaultContainer /p:RuntimeIdentifier=linux-x64 /p:PublishTrimmed=true /p:SuppressTrimAnalysisWarnings=true ./Mqtt.Server
```
> [!TIP]
>
> **/p:RuntimeIdentifier** switch denotes target container runtime identifier in the form *[OS name]-[architecture]* e.g.:
>
>* **/p:RuntimeIdentifier=linux-x64** - build Linux image for x64
>* **/p:RuntimeIdentifier=linux-arm64** - build Linux image for ARM64
>* **/p:RuntimeIdentifier=linux-musl-x64** - build Linux image for x64, but use slim 'musl libc' based Alpine distro as base image


Build container image with name 'docker_user/mqtt-server' and publish to the online registry (docker.io e.g.)

```
dotnet publish /p:Configuration=Release /p:PublishProfile=DefaultContainer /p:RuntimeIdentifier=linux-x64 /p:PublishTrimmed=true /p:SuppressTrimAnalysisWarnings=true /p:ContainerRepository='docker_user/mqtt-server' /p:ContainerRegistry='docker.io' ./Mqtt.Server
```

It is also possible to build and publish multi-architecture images via Docker image manifest files:

```
dotnet build /t:PublishAllImages /p:Configuration=Release /p:PublishProfile=DefaultContainer /p:RuntimeIdentifiers='"linux-x64;linux-arm64"' /p:ContainerRepository='docker_user/mqtt-server' /p:ContainerRegistry='docker.io' /p:PublishTrimmed=true /p:SelfContained=true /p:SuppressTrimAnalysisWarnings=true ./Mqtt.Server
```

### Run container from image:

```
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

If you need to persist application data and be able to customize default configuration, just bind container's **/home/app** directory to some persisting host's directory:
```
docker run --volume=<some_host_directory_to_store_container_data>:/home/app -p 1883:1883 -p 8001:8001 -p 8002:8002 mqtt-server:latest
```

HTTPS and MQTT TCP SSL can be also configured additionally via environment variables:
```
docker run --env=Kestrel__Endpoints__https__Url=https://*:8002 --env=Kestrel__Certificates__Default__Path=/home/app/.config/mqtt-server/mqtt-server.pfx --env=MQTT__Endpoints__tcp.ssl.default__Url=tcps://[::]:8883 --env=MQTT__Endpoints__tcp.ssl.default__Certificate__Path=/home/app/.config/mqtt-server/mqtt-server.pfx --volume=<some_host_directory_to_store_container_data>:/home/app -p 1883:1883 -p 8001:8001 -p 8002:8002 -p 8003:8003 -p 8883:8883 mqtt-server:latest
```

If you want to know how to generate self-signed SSL certificate on the app startup, please refer to:

[Example Docker Compose file](Mqtt.Server/docker-compose.yml)