using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.Runtime;

namespace Soenneker.Utils.Paths.Resources;

/// <summary>
/// Resolves the absolute path to a <c>Resources</c> folder across environments.
/// Resolution order:
/// 1) <c>RESOURCES_DIR</c> env var (if set and exists)
/// 2) Build output: <c>AppContext.BaseDirectory/Resources</c>
/// 3) Azure Functions/App Service: <c>HOME/site/wwwroot/Resources</c>
/// 4) GitHub Actions: nearest <c>Resources</c> from CWD (bounded by <c>GITHUB_WORKSPACE</c>); fallback to <c>GITHUB_WORKSPACE/Resources</c>
/// 5) Generic containers: <c>AppContext.BaseDirectory/Resources</c>
/// 6) Generic dev/test: nearest <c>Resources</c> from CWD
/// 7) If <c>HOME</c> exists: <c>HOME/site/wwwroot/Resources</c>
/// 8) Final fallback: <c>AppContext.BaseDirectory/Resources</c> (may not exist)
/// </summary>
public static class ResourcesPathUtil
{
    private const string _resourcesFolderName = "Resources";
    private const string _overrideEnv = "RESOURCES_DIR";

    private static volatile string? _cached;

    /// <summary>
    /// Returns the absolute path to the "Resources" directory according to the resolution order above.
    /// </summary>
    [Pure]
    public static async ValueTask<string> Get(CancellationToken cancellationToken = default)
    {
        string? cached = _cached;

        if (cached is not null)
            return cached;

        // 1) Explicit override
        string? overrideDir = Environment.GetEnvironmentVariable(_overrideEnv);
        if (overrideDir.HasContent() && Directory.Exists(overrideDir))
            return _cached = Path.GetFullPath(overrideDir);

        // 2) Build output
        string baseDir = AppContext.BaseDirectory;
        string baseResources = Path.Combine(baseDir, _resourcesFolderName);

        if (Directory.Exists(baseResources))
            return _cached = baseResources;

        // 3) Azure Functions/App Service
        if (RuntimeUtil.IsAzureFunction || RuntimeUtil.IsAzureAppService)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string azureResources = Path.Combine(home, "site", "wwwroot", _resourcesFolderName);

                if (Directory.Exists(azureResources))
                    return _cached = azureResources;
            }
        }

        // 4) GitHub Actions
        if (RuntimeUtil.IsGitHubAction)
        {
            string? workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            string cwd = Directory.GetCurrentDirectory();

            string? boundRoot = null;
            if (workspace.HasContent() && Directory.Exists(workspace))
                boundRoot = Path.GetFullPath(workspace);

            string? foundInActions = FindUpForDirectory(cwd, _resourcesFolderName, boundRoot);
            if (foundInActions is not null)
                return _cached = foundInActions;

            if (boundRoot is not null)
            {
                string repoResources = Path.Combine(boundRoot, _resourcesFolderName);

                if (Directory.Exists(repoResources))
                    return _cached = repoResources;
            }
        }

        // 5) Generic containers
        if (await RuntimeUtil.IsContainer(cancellationToken).NoSync())
        {
            string containerResources = Path.Combine(baseDir, _resourcesFolderName);
            if (Directory.Exists(containerResources))
                return _cached = containerResources;
        }

        // 6) Generic dev/test
        {
            string cwd = Directory.GetCurrentDirectory();
            string? found = FindUpForDirectory(cwd, _resourcesFolderName, stopAtInclusive: null);

            if (found is not null)
                return _cached = found;
        }

        // 7) HOME/site/wwwroot/Resources (non-Azure)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home.HasContent())
            {
                string homeResources = Path.Combine(home, "site", "wwwroot", _resourcesFolderName);

                if (Directory.Exists(homeResources))
                    return _cached = homeResources;
            }
        }

        // 8) Last resort
        return _cached = baseResources;
    }

    /// <summary>Absolute path to a file under /Resources.</summary>
    [Pure]
    public static async ValueTask<string> GetResourceFilePath(string fileName, CancellationToken cancellationToken = default)
    {
        string resourcesPath = await Get(cancellationToken).NoSync();
        return Path.Combine(resourcesPath, fileName);
    }

    private static string? FindUpForDirectory(string startDir, string targetName, string? stopAtInclusive)
    {
        string current = TrimEndSeparators(Path.GetFullPath(startDir));
        string? stop = stopAtInclusive is null ? null : TrimEndSeparators(Path.GetFullPath(stopAtInclusive));

        while (true)
        {
            string candidate = Path.Combine(current, targetName);
            if (Directory.Exists(candidate))
                return candidate;

            if (stop is not null && current.EqualsIgnoreCase(stop))
                break;

            string? parent = Directory.GetParent(current)?.FullName;
            if (parent is null)
                break;

            current = TrimEndSeparators(parent);
        }

        return null;
    }

    private static string TrimEndSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
