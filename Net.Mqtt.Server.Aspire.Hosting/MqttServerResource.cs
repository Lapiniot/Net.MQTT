using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Net.Mqtt.Server.Aspire.Hosting;

public class MqttServerResource(string name) : ContainerResource(name),
    IResourceWithConnectionString,
    IResourceWithServiceDiscovery,
    IContainerFilesDestinationResource
{
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"mqtt://{this.GetEndpoint("mqtt").Property(EndpointProperty.HostAndPort)}");
}