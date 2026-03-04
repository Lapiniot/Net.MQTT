using System.Diagnostics;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1812 // Internal class is never instantiated
#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

internal sealed partial class ContainerImageManager(ILogger<ContainerImageManager> logger,
    IServiceProvider serviceProvider, DistributedApplicationExecutionContext? executionContext = null) :
    IResourceContainerImageManager
{
    private readonly ILogger<ContainerImageManager> logger = logger;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly DistributedApplicationExecutionContext? executionContext = executionContext;

    public async Task BuildImageAsync(IResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is ProjectResource)
        {
            LogBuilding(resource.Name);

            if (!resource.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
            {
                throw new DistributedApplicationException($"The resource '{resource.Name}' does not have a project metadata annotation.");
            }

            var projectPath = projectMetadata.ProjectPath;
            var arguments = await BuildPublishArgumentsAsync(resource, cancellationToken).ConfigureAwait(false);

            LogBuildStarting(arguments);

            var exitCode = await ExecuteDotNetPublishAsync(projectPath, arguments, cancellationToken).ConfigureAwait(false);

            if (exitCode is not 0)
            {
                LogBuildFailed(projectPath, exitCode);
                LogBuildingFailed(resource.Name);
                throw new DistributedApplicationException($"Failed to build container image.");
            }
            else
            {
                LogBuildCompleted(exitCode);
                LogBuildingComplete(resource.Name);
            }
        }
        else
        {
            await GetAspireImpl().BuildImageAsync(resource, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken = default)
    {
        return GetAspireImpl().BuildImagesAsync(resources, cancellationToken);
    }

    public Task PushImageAsync(IResource resource, CancellationToken cancellationToken)
    {
        return GetAspireImpl().PushImageAsync(resource, cancellationToken);
    }

    private async Task<string> BuildPublishArgumentsAsync(IResource resource, CancellationToken cancellationToken)
    {
        var context = new ContainerBuildOptionsCallbackContext(resource, serviceProvider,
            logger, cancellationToken, executionContext);

        if (resource.TryGetAnnotationsOfType<ContainerBuildOptionsCallbackAnnotation>(out var annotations))
        {
            foreach (var annotation in annotations)
            {
                await annotation.Callback(context).ConfigureAwait(false);
            }
        }

        var containerRepository = context.LocalImageName ?? resource.Name;
        var containerImageTag = context.LocalImageTag ?? "latest";

        var arguments = $"publish --configuration=Release /t:PublishContainer /p:ContainerRepository=\"{containerRepository}\" /p:ContainerImageTag=\"{containerImageTag}\"";

        if (context.TargetPlatform is { } platform)
        {
            var rids = GetRuntimeIdentifiers(platform);

            if (rids is [var rid])
            {
                arguments += $" /p:RuntimeIdentifier=\"{rid}\" /p:ContainerRuntimeIdentifier=\"{rid}\"";
            }
            else
            {
                arguments += $" /p:RuntimeIdentifiers=\\\"{string.Join(';', rids)}\\\" /p:ContainerRuntimeIdentifiers=\\\"{string.Join(';', rids)}\\\"";
            }
        }

        if (context.OutputPath is { } outPath)
        {
            arguments += $" /p:ContainerArchiveOutputPath=\"{outPath}\"";
        }

        if (context.ImageFormat is { } format)
        {
            arguments += $" /p:ContainerImageFormat=\"{GetContainerFormat(format)}\"";
        }

        if (resource.TryGetLastAnnotation<DockerfileBaseImageAnnotation>(out var baseImageAnnotation) &&
            baseImageAnnotation.RuntimeImage is { Length: > 0 } baseImage)
        {
            arguments += $" /p:ContainerBaseImage=\"{baseImage}\"";
        }

        if (resource.TryGetAnnotationsOfType<ContainerImageDotNetPublishArgsCallbackAnnotation>(out var argsAnnotations))
        {
            var argsContext = new ContainerImageDotNetPublishArgsCallbackContext(arguments: [],
                resource, serviceProvider, logger, cancellationToken, executionContext);

            foreach (var argsAnnotation in argsAnnotations)
            {
                await argsAnnotation.Callback(argsContext).ConfigureAwait(false);
            }

            foreach (var argument in argsContext.Arguments)
            {
                if (await ResolveValueAsync(argument, cancellationToken).ConfigureAwait(false) is { Length: > 0 } value)
                {
                    arguments += $" {value}";
                }
            }
        }

        return arguments;
    }

    private async Task<int> ExecuteDotNetPublishAsync(string projectPath, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = $"{arguments} \"{projectPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += OnOutputDataReceived;
        process.ErrorDataReceived += OnErrorDataReceived;

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
        finally
        {
            process.OutputDataReceived -= OnOutputDataReceived;
            process.ErrorDataReceived -= OnErrorDataReceived;
        }

        void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is string { Length: > 0 } output)
            {
                LogBuildOutput(projectPath, output);
            }
        }

        void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is string { Length: > 0 } error)
            {
                LogBuildError(projectPath, error);
            }
        }

        var exitCode = process.ExitCode;

        if (exitCode is not 0)
        {
            LogProcessError(process.StartInfo.FileName, process.StartInfo.Arguments, exitCode);
        }

        return exitCode;
    }

    private IResourceContainerImageManager GetAspireImpl()
    {
        return serviceProvider
            .GetServices<IResourceContainerImageManager>()
            .Last(impl => impl is not ContainerImageManager);
    }

    private static async Task<string?> ResolveValueAsync(object argument, CancellationToken cancellationToken)
    {
        return argument switch
        {
            string strValue => strValue,
            IValueProvider valueProvider => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
            _ => argument.ToString()
        };
    }

    private static string GetContainerFormat(ContainerImageFormat format)
    {
        return format switch
        {
            ContainerImageFormat.Docker => "Docker",
            ContainerImageFormat.Oci => "OCI",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Invalid container image format")
        };
    }

    private static List<string> GetRuntimeIdentifiers(ContainerTargetPlatform platform)
    {
        List<string> rids = [];

        if (platform.HasFlag(ContainerTargetPlatform.LinuxAmd64))
        {
            rids.Add("linux-x64");
        }
        else if (platform.HasFlag(ContainerTargetPlatform.LinuxArm64))
        {
            rids.Add("linux-arm64");
        }
        else if (platform.HasFlag(ContainerTargetPlatform.Linux386))
        {
            rids.Add("linux-x86");
        }
        else if (platform.HasFlag(ContainerTargetPlatform.LinuxArm))
        {
            rids.Add("linux-arm");
        }
        else if (platform.HasFlag(ContainerTargetPlatform.WindowsAmd64))
        {
            rids.Add("win-x64");
        }
        else if (platform.HasFlag(ContainerTargetPlatform.WindowsArm64))
        {
            rids.Add("win-arm64");
        }

        return rids is []
            ? throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported container target platform")
            : rids;
    }

    [LoggerMessage(LogLevel.Error, "Command {executablePath} {arguments} returned non-zero exit code {exitCode}")]
    private partial void LogProcessError(string executablePath, string Arguments, int exitCode);

    [LoggerMessage(LogLevel.Information, "Building image: {ResourceName}")]
    private partial void LogBuilding(string resourceName);

    [LoggerMessage(LogLevel.Information, "Building image for {ResourceName} completed")]
    private partial void LogBuildingComplete(string resourceName);

    [LoggerMessage(LogLevel.Information, "Building image for {ResourceName} failed")]
    private partial void LogBuildingFailed(string resourceName);

    [LoggerMessage(LogLevel.Debug, "dotnet publish {ProjectPath} (stdout): {Output}")]
    private partial void LogBuildOutput(string projectPath, string output);

    [LoggerMessage(LogLevel.Error, "dotnet publish {ProjectPath} (stderr): {Error}")]
    private partial void LogBuildError(string projectPath, string error);

    [LoggerMessage(LogLevel.Debug, "Starting .NET CLI with arguments: {Arguments}")]
    private partial void LogBuildStarting(string arguments);

    [LoggerMessage(LogLevel.Debug, ".NET CLI completed with exit code: {ExitCode}")]
    private partial void LogBuildCompleted(int exitCode);

    [LoggerMessage(LogLevel.Error, "dotnet publish for project {ProjectPath} failed with exit code {ExitCode}.")]
    private partial void LogBuildFailed(string projectPath, int exitCode);
}