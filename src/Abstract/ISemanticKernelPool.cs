using Microsoft.SemanticKernel;
using Soenneker.SemanticKernel.Dtos.Options;
using Soenneker.SemanticKernel.Enums.KernelType;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool.Abstract;

/// <summary>
/// Provides a thread-safe pool of Semantic Kernel instances with per-entry rate limiting and lifecycle control.
/// Enables dynamic registration, retrieval, quota tracking, and disposal of kernel instances by key.
/// </summary>
public interface ISemanticKernelPool
{
    /// <summary>
    /// Retrieves the next available <see cref="Kernel"/> from the pool based on rate-limiting availability.
    /// If no type is specified, defaults to <see cref="KernelType.Chat"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="cancellationToken">A token to observe while waiting for an available kernel.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><see cref="Kernel"/>: The Semantic Kernel instance if one is available.</description></item>
    /// <item><description><see cref="IKernelPoolEntry"/>: The associated metadata entry for the kernel.</description></item>
    /// </list>
    /// If no kernel is available before cancellation, both values will be null.
    /// </returns>
    ValueTask<(Kernel? kernel, IKernelPoolEntry? entry)> GetAvailableKernel(KernelType? type = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the remaining rate-limited quota for all registered kernel entries in the pool.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ConcurrentDictionary{TKey, TValue}"/> mapping registered keys to their respective remaining quotas,
    /// represented as a tuple of (second, minute, day) counts.
    /// </returns>
    ValueTask<ConcurrentDictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new Semantic Kernel entry in the pool using the provided configuration options.
    /// </summary>
    /// <param name="key">A unique string key used to identify and retrieve the registered entry.</param>
    /// <param name="options">Configuration options that define the kernel behavior, rate limits, and endpoint.</param>
    /// <param name="cancellationToken">A token to cancel the registration process.</param>
    ValueTask Register(string key, SemanticKernelOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an existing <see cref="IKernelPoolEntry"/> in the pool under the specified key.
    /// </summary>
    /// <param name="key">A unique string key used to identify and retrieve the registered entry.</param>
    /// <param name="entry">An instance of <see cref="IKernelPoolEntry"/> representing the kernel entry to register.</param>
    /// <param name="cancellationToken">A token to cancel the registration process.</param>
    ValueTask Register(string key, IKernelPoolEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a kernel entry from the pool by key, removing its availability and clearing any associated kernel cache.
    /// </summary>
    /// <param name="key">The unique key of the kernel entry to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the entry was successfully removed; otherwise, false.</returns>
    ValueTask<bool> Unregister(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to retrieve a registered <see cref="IKernelPoolEntry"/> by its key without checking availability.
    /// </summary>
    /// <param name="key">The unique key of the entry to retrieve.</param>
    /// <param name="entry">When this method returns, contains the associated entry if found; otherwise, null.</param>
    /// <returns>True if the entry was found; otherwise, false.</returns>
    bool TryGet(string key, out IKernelPoolEntry? entry);

    /// <summary>
    /// Clears all registered kernel entries and their associated rate limits and cache entries.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask Clear(CancellationToken cancellationToken = default);
}