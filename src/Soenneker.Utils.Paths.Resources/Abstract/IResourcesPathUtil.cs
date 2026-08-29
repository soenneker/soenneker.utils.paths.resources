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
    /// Absolute path to a file under /Resources.
    /// </summary>
    /// <returns>Absolute path to a file under /Resources.</returns>
    ValueTask<string> GetResourceFilePath(string fileName, CancellationToken cancellationToken = default);
}
