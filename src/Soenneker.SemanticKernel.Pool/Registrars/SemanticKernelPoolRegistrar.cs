using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.SemanticKernel.Cache.Registrars;
using Soenneker.SemanticKernel.Pool.Abstract;

namespace Soenneker.SemanticKernel.Pool.Registrars;

/// <summary>
/// Extension methods for registering the SemanticKernelPool.
/// </summary>
public static class SemanticKernelPoolRegistrar
{
    /// <summary>
    /// Registers the SemanticKernelPool as a singleton service.
    /// </summary>
    public static IServiceCollection AddSemanticKernelPoolAsSingleton(this IServiceCollection services)
    {
        services.AddSemanticKernelCacheAsSingleton().TryAddSingleton<ISemanticKernelPool, SemanticKernelPool>();
        return services;
    }

    /// <summary>
    /// Registers the SemanticKernelPool as a scoped service.
    /// </summary>
    public static IServiceCollection AddSemanticKernelPoolAsScoped(this IServiceCollection services)
    {
        services.AddSemanticKernelCacheAsSingleton().TryAddScoped<ISemanticKernelPool, SemanticKernelPool>();
        return services;
    }
}