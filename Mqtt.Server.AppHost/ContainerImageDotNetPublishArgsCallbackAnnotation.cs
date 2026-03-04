using Microsoft.Extensions.Logging;

internal sealed class ContainerImageDotNetPublishArgsCallbackAnnotation(Func<ContainerImageDotNetPublishArgsCallbackContext, Task> callback)
    : IResourceAnnotation
{
    public Func<ContainerImageDotNetPublishArgsCallbackContext, Task> Callback { get; } = callback;
}

#pragma warning disable CA1068 // CancellationToken parameters must come last
internal sealed class ContainerImageDotNetPublishArgsCallbackContext(IList<object> arguments,
    IResource resource, IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken,
    DistributedApplicationExecutionContext? executionContext)
#pragma warning restore CA1068 // CancellationToken parameters must come last
{
    public IList<object> Arguments { get; } = arguments;
    public IResource Resource { get; } = resource;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public ILogger Logger { get; } = logger;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public DistributedApplicationExecutionContext? ExecutionContext { get; } = executionContext;
}