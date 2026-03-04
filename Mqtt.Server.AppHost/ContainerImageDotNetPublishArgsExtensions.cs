using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

internal static class ContainerImageDotNetPublishArgsExtensions
{
    extension<T>(IResourceBuilder<T> resourceBuilder) where T : ProjectResource
    {
        public IResourceBuilder<T> PublishWithDotNetPublishArgs(Func<ContainerImageDotNetPublishArgsCallbackContext, Task> callback)
        {
            if (resourceBuilder.ApplicationBuilder is { ExecutionContext.IsPublishMode: true } builder)
            {
                builder.Services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<IResourceContainerImageManager, ContainerImageManager>());
                return resourceBuilder.WithAnnotation(new ContainerImageDotNetPublishArgsCallbackAnnotation(callback));
            }

            return resourceBuilder;
        }

        public IResourceBuilder<T> PublishWithDotNetPublishArgs(Action<IList<object>> callback)
        {
            return resourceBuilder.PublishWithDotNetPublishArgs(ctx =>
                {
                    callback(ctx.Arguments);
                    return Task.CompletedTask;
                });
        }

        public IResourceBuilder<T> PublishWithDotNetPublishArgs(params IReadOnlyList<object> arguments)
        {
            return resourceBuilder.PublishWithDotNetPublishArgs(args =>
                {
                    foreach (var argument in arguments)
                    {
                        args.Add(argument);
                    }
                });
        }
    }
}