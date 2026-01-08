using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Atomics.Strings;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.Runtime;

namespace Soenneker.Utils.Paths.Resources;

public static class ResourcesPathUtil
{
    private const string _resourcesFolderName = "Resources";
    private const string _overrideEnv = "RESOURCES_DIR";

    private static readonly AtomicString _cached = new();

    /// <summary>
    /// Returns the absolute path to the "Resources" directory according to the resolution order.
    /// </summary>
    [Pure]
    public static ValueTask<string> Get(CancellationToken cancellationToken = default)
    {
        // Fastest path: no async state machine once cached.
        string? cached = _cached.Get();
        if (cached is not null)
            return new ValueTask<string>(cached);

        // Only allocate/await when not cached.
        return GetSlow(cancellationToken);

        static async ValueTask<string> GetSlow(CancellationToken ct)
        {
            // If someone else wins the race while we're awaiting, we return the winner.
            // AtomicString doesn't have async GetOrAdd, so we do best-effort publish with TrySet.
            string resolved = await Resolve(ct)
                .NoSync();

            if (_cached.TrySet(resolved))
                return resolved;

            return _cached.Get()!; // winner
        }
    }

    /// <summary>Absolute path to a file under /Resources.</summary>
    [Pure]
    public static async ValueTask<string> GetResourceFilePath(string fileName, CancellationToken cancellationToken = default)
    {
        string resourcesPath = await Get(cancellationToken)
            .NoSync();
        return Path.Combine(resourcesPath, fileName);
    }

    private static async ValueTask<string> Resolve(CancellationToken cancellationToken)
    {
        string baseDir = AppContext.BaseDirectory;
        string baseResources = Path.Combine(baseDir, _resourcesFolderName);
        string cwd = Directory.GetCurrentDirectory();

        // 1) Explicit override
        string? overrideDir = Environment.GetEnvironmentVariable(_overrideEnv);
        if (overrideDir.HasContent() && Directory.Exists(overrideDir))
            return Path.GetFullPath(overrideDir);

        // 2) Build output
        if (Directory.Exists(baseResources))
            return baseResources;

        // 3) Azure Functions/App Service
        if (RuntimeUtil.IsAzureFunction || RuntimeUtil.IsAzureAppService)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string azureResources = Path.Combine(home, "site", "wwwroot", _resourcesFolderName);
                if (Directory.Exists(azureResources))
                    return azureResources;
            }
        }

        // 4) GitHub Actions
        if (RuntimeUtil.IsGitHubAction)
        {
            string? workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

            string? boundRoot = null;
            if (workspace.HasContent() && Directory.Exists(workspace))
                boundRoot = Path.GetFullPath(workspace);

            string? foundInActions = FindUpForDirectory(cwd, _resourcesFolderName, boundRoot);
            if (foundInActions is not null)
                return foundInActions;

            if (boundRoot is not null)
            {
                string repoResources = Path.Combine(boundRoot, _resourcesFolderName);
                if (Directory.Exists(repoResources))
                    return repoResources;
            }
        }

        // 5) Generic containers
        if (await RuntimeUtil.IsContainer(cancellationToken)
                             .NoSync())
        {
            // baseResources already computed
            if (Directory.Exists(baseResources))
                return baseResources;
        }

        // 6) Generic dev/test
        {
            string? found = FindUpForDirectory(cwd, _resourcesFolderName, stopAtInclusive: null);
            if (found is not null)
                return found;
        }

        // 7) HOME/site/wwwroot/Resources (non-Azure)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string homeResources = Path.Combine(home, "site", "wwwroot", _resourcesFolderName);
                if (Directory.Exists(homeResources))
                    return homeResources;
            }
        }

        // 8) Last resort
        return baseResources;
    }

    private static string? FindUpForDirectory(string startDir, string targetName, string? stopAtInclusive)
    {
        string current = NormalizeDir(startDir);
        string? stop = stopAtInclusive is null ? null : NormalizeDir(stopAtInclusive);

        while (true)
        {
            // allocs one string per level (unavoidable for Directory.Exists(string))
            string candidate = Path.Combine(current, targetName);
            if (Directory.Exists(candidate))
                return candidate;

            if (stop is not null && string.Equals(current, stop, StringComparison.OrdinalIgnoreCase))
                return null;

            // Avoid Directory.GetParent (DirectoryInfo alloc); use string-based path op
            string? parent = Path.GetDirectoryName(current);
            if (parent is null)
                return null;

            current = NormalizeDir(parent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeDir(string path)
    {
        string full = Path.GetFullPath(path);

        // TrimEnd only allocates if there are trailing separators.
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}