namespace Net.Mqtt.Server.AspNetCore.Hosting;

internal sealed class EndpointRouteBuilderProxy<TState>(IEndpointRouteBuilder endpoints,
    Action<IApplicationBuilder, TState> configure, TState state) : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider => endpoints.ServiceProvider;

    public ICollection<EndpointDataSource> DataSources => endpoints.DataSources;

    public IApplicationBuilder CreateApplicationBuilder()
    {
        var applicationBuilder = endpoints.CreateApplicationBuilder();
        configure(applicationBuilder, state);
        return applicationBuilder;
    }
}