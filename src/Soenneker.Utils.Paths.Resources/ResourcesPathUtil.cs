using Soenneker.Atomics.Strings;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Paths.Resources.Abstract;
using Soenneker.Utils.Runtime;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.Paths.Resources;

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

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<string> Get(CancellationToken cancellationToken = default)
    {
        string? cached = _cached.Get();
        return cached is not null ? new ValueTask<string>(cached) : GetSlow(cancellationToken);
    }

    private async ValueTask<string> GetSlow(CancellationToken cancellationToken)
    {
        string resolved = await Resolve(cancellationToken)
            .NoSync();

        if (_cached.TrySet(resolved))
            return resolved;

        return _cached.Get()!;
    }

    [Pure]
    public async ValueTask<string> GetResourceFilePath(string fileName, CancellationToken cancellationToken = default)
    {
        string resourcesPath = await Get(cancellationToken)
            .NoSync();
        return System.IO.Path.Combine(resourcesPath, fileName);
    }

    private async ValueTask<string> Resolve(CancellationToken cancellationToken)
    {
        string baseDir = AppContext.BaseDirectory;
        string baseResources = System.IO.Path.Combine(baseDir, _resourcesFolderName);
        string cwd = System.IO.Directory.GetCurrentDirectory();

        // 1) Explicit override
        string? overrideDir = Environment.GetEnvironmentVariable(_overrideEnv);
        if (overrideDir.HasContent() && await _directoryUtil.Exists(overrideDir, cancellationToken)
                                                            .NoSync())
            return NormalizeOnce(overrideDir);

        // 2) Build output (common case)
        if (await _directoryUtil.Exists(baseResources, cancellationToken)
                                .NoSync())
            return baseResources;

        // 3) Azure Functions/App Service
        if (RuntimeUtil.IsAzureFunction || RuntimeUtil.IsAzureAppService)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string azureResources = System.IO.Path.Combine(home, "site", "wwwroot", _resourcesFolderName);
                if (await _directoryUtil.Exists(azureResources, cancellationToken)
                                        .NoSync())
                    return azureResources;
            }
        }

        // 4) GitHub Actions
        if (RuntimeUtil.IsGitHubAction)
        {
            string? workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

            string? boundRoot = null;
            if (workspace.HasContent() && await _directoryUtil.Exists(workspace, cancellationToken)
                                                              .NoSync())
                boundRoot = NormalizeOnce(workspace);

            string? foundInActions = await FindUpForDirectory(cwd, _resourcesFolderName, boundRoot, cancellationToken)
                .NoSync();
            if (foundInActions is not null)
                return foundInActions;

            if (boundRoot is not null)
            {
                string repoResources = System.IO.Path.Combine(boundRoot, _resourcesFolderName);
                if (await _directoryUtil.Exists(repoResources, cancellationToken)
                                        .NoSync())
                    return repoResources;
            }
        }

        // 5) Generic containers (no redundant baseResources check)
        if (await RuntimeUtil.IsContainer(cancellationToken)
                             .NoSync())
        {
            // already checked baseResources above; if containers have another convention, add it here.
        }

        // 6) Generic dev/test
        {
            string? found = await FindUpForDirectory(cwd, _resourcesFolderName, stopAtInclusive: null, cancellationToken)
                .NoSync();
            if (found is not null)
                return found;
        }

        // 7) HOME/site/wwwroot/Resources (non-Azure)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string homeResources = System.IO.Path.Combine(home, "site", "wwwroot", _resourcesFolderName);
                if (await _directoryUtil.Exists(homeResources, cancellationToken)
                                        .NoSync())
                    return homeResources;
            }
        }

        // 8) Last resort
        return baseResources;
    }

    private async ValueTask<string?> FindUpForDirectory(string startDir, string targetName, string? stopAtInclusive, CancellationToken cancellationToken)
    {
        // Normalize once up front.
        string current = NormalizeOnce(startDir);

        string? stop = stopAtInclusive is null ? null : NormalizeOnce(stopAtInclusive);

        while (true)
        {
            string candidate = System.IO.Path.Combine(current, targetName);

            if (await _directoryUtil.Exists(candidate, cancellationToken)
                                    .NoSync())
                return candidate;

            if (stop is not null && string.Equals(current, stop, StringComparison.OrdinalIgnoreCase))
                return null;

            // GetDirectoryName is already "normalized enough" for upward traversal.
            string? parent = System.IO.Path.GetDirectoryName(current);
            if (parent is null)
                return null;

            // Avoid re-full-pathing every time; just trim the trailing separator if present.
            current = System.IO.Path.TrimEndingDirectorySeparator(parent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeOnce(string path)
    {
        // FullPath once + trim once.
        string full = System.IO.Path.GetFullPath(path);
        return System.IO.Path.TrimEndingDirectorySeparator(full);
    }
}