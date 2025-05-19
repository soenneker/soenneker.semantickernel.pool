using Microsoft.SemanticKernel;
using Soenneker.SemanticKernel.Dtos.Options;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool.Abstract;

/// <summary>
/// Manages a pool of Semantic Kernel instances with per-entry rate limiting.
/// </summary>
public interface ISemanticKernelPool
{
    /// <summary>
    /// Gets the next available kernel based on rate limits.
    /// </summary>
    /// <param name="cancellationToken">A cancellation cancellationToken that can be used to cancel the operation.</param>
    /// <returns>A tuple containing the kernel and its entry if available, null otherwise.</returns>
    ValueTask<(Kernel? kernel, IKernelPoolEntry? entry)> GetAvailableKernel(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining quota for all registered kernels.
    /// </summary>
    /// <returns>A dictionary mapping kernel keys to their remaining quotas.</returns>
    ValueTask<ConcurrentDictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(CancellationToken cancellationToken = default);

    ValueTask Register(string key, SemanticKernelOptions options, CancellationToken cancellationToken = default);

    ValueTask Register(string key, IKernelPoolEntry entry, CancellationToken cancellationToken = default);

    ValueTask<bool> Unregister(string key, CancellationToken cancellationToken = default);

    bool TryGet(string key, out IKernelPoolEntry? entry);
} 