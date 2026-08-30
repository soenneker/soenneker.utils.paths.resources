using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.Paths.Resources.Abstract;

/// <summary>
/// Resolves the absolute path to the Resources directory and resource file paths.
/// </summary>
public interface IResourcesPathUtil
{
    /// <summary>
    /// Returns the absolute path to the "Resources" directory according to the resolution order.
    /// </summary>
    /// <returns>The absolute path to the "Resources" directory according to the resolution order.</returns>
    ValueTask<string> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a relative file path beneath the selected Resources directory without allowing traversal outside it.
    /// </summary>
    /// <param name="fileName">A relative file or nested path beneath Resources.</param>
    /// <param name="cancellationToken">Signals that resolution should stop.</param>
    /// <returns>The absolute contained path. The file is not required to exist.</returns>
    ValueTask<string> GetResourceFilePath(string fileName, CancellationToken cancellationToken = default);
}
