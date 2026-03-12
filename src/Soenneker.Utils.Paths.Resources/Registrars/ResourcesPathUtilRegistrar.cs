using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.Directory.Registrars;
using Soenneker.Utils.Paths.Resources.Abstract;

namespace Soenneker.Utils.Paths.Resources.Registrars;

public static class ResourcesPathUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IResourcesPathUtil"/> as a scoped service.
    /// </summary>
    public static IServiceCollection AddResourcesPathUtilAsScoped(this IServiceCollection services)
    {
        services.AddDirectoryUtilAsScoped().TryAddScoped<IResourcesPathUtil, ResourcesPathUtil>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="IResourcesPathUtil"/> as a singleton service.
    /// </summary>
    public static IServiceCollection AddResourcesPathUtilAsSingleton(this IServiceCollection services)
    {
        services.AddDirectoryUtilAsSingleton().TryAddSingleton<IResourcesPathUtil, ResourcesPathUtil>();
        return services;
    }
}
