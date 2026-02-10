using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Atomics.Strings;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Paths.Resources.Abstract;
using Soenneker.Utils.Runtime;

namespace Soenneker.Utils.Paths.Resources;

/// <inheritdoc cref="IResourcesPathUtil"/>
public sealed class ResourcesPathUtil : IResourcesPathUtil
{
    private const string _resourcesFolderName = "Resources";
    private const string _overrideEnv = "RESOURCES_DIR";

    private readonly IDirectoryUtil _directoryUtil;
    private readonly AtomicString _cached = new();

    public ResourcesPathUtil(IDirectoryUtil directoryUtil)
    {
        _directoryUtil = directoryUtil;
    }

    /// <inheritdoc />
    [Pure]
    public ValueTask<string> Get(CancellationToken cancellationToken = default)
    {
        string? cached = _cached.Get();
        if (cached is not null)
            return new ValueTask<string>(cached);

        return GetSlow(cancellationToken);
    }

    private async ValueTask<string> GetSlow(CancellationToken cancellationToken)
    {
        string resolved = await Resolve(cancellationToken);

        if (_cached.TrySet(resolved))
            return resolved;

        return _cached.Get()!;
    }

    [Pure]
    public async ValueTask<string> GetResourceFilePath(string fileName, CancellationToken cancellationToken = default)
    {
        string resourcesPath = await Get(cancellationToken).NoSync();
        return System.IO.Path.Combine(resourcesPath, fileName);
    }

    private async ValueTask<string> Resolve(CancellationToken cancellationToken)
    {
        string baseDir = AppContext.BaseDirectory;
        string baseResources = System.IO.Path.Combine(baseDir, _resourcesFolderName);
        string cwd = System.IO.Directory.GetCurrentDirectory();

        // 1) Explicit override
        string? overrideDir = Environment.GetEnvironmentVariable(_overrideEnv);
        if (overrideDir.HasContent() && await _directoryUtil.Exists(overrideDir, cancellationToken))
            return System.IO.Path.GetFullPath(overrideDir);

        // 2) Build output
        if (await _directoryUtil.Exists(baseResources, cancellationToken))
            return baseResources;

        // 3) Azure Functions/App Service
        if (RuntimeUtil.IsAzureFunction || RuntimeUtil.IsAzureAppService)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string azureResources = System.IO.Path.Combine(home, "site", "wwwroot", _resourcesFolderName);
                if (await _directoryUtil.Exists(azureResources, cancellationToken))
                    return azureResources;
            }
        }

        // 4) GitHub Actions
        if (RuntimeUtil.IsGitHubAction)
        {
            string? workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

            string? boundRoot = null;
            if (workspace.HasContent() && await _directoryUtil.Exists(workspace, cancellationToken))
                boundRoot = System.IO.Path.GetFullPath(workspace);

            string? foundInActions = await FindUpForDirectory(cwd, _resourcesFolderName, boundRoot, cancellationToken);
            if (foundInActions is not null)
                return foundInActions;

            if (boundRoot is not null)
            {
                string repoResources = System.IO.Path.Combine(boundRoot, _resourcesFolderName);
                if (await _directoryUtil.Exists(repoResources, cancellationToken))
                    return repoResources;
            }
        }

        // 5) Generic containers
        if (await RuntimeUtil.IsContainer(cancellationToken)
                             .NoSync())
        {
            if (await _directoryUtil.Exists(baseResources, cancellationToken))
                return baseResources;
        }

        // 6) Generic dev/test
        {
            string? found = await FindUpForDirectory(cwd, _resourcesFolderName, stopAtInclusive: null, cancellationToken);
            if (found is not null)
                return found;
        }

        // 7) HOME/site/wwwroot/Resources (non-Azure)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string homeResources = System.IO.Path.Combine(home, "site", "wwwroot", _resourcesFolderName);
                if (await _directoryUtil.Exists(homeResources, cancellationToken))
                    return homeResources;
            }
        }

        // 8) Last resort
        return baseResources;
    }

    private async ValueTask<string?> FindUpForDirectory(string startDir, string targetName, string? stopAtInclusive, CancellationToken cancellationToken)
    {
        string current = NormalizeDir(startDir);
        string? stop = stopAtInclusive is null ? null : NormalizeDir(stopAtInclusive);

        while (true)
        {
            string candidate = System.IO.Path.Combine(current, targetName);
            if (await _directoryUtil.Exists(candidate, cancellationToken))
                return candidate;

            if (stop is not null && string.Equals(current, stop, StringComparison.OrdinalIgnoreCase))
                return null;

            string? parent = System.IO.Path.GetDirectoryName(current);
            if (parent is null)
                return null;

            current = NormalizeDir(parent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeDir(string path)
    {
        string full = System.IO.Path.GetFullPath(path);
        return full.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
    }
}
