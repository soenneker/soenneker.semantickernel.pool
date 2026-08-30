using Microsoft.SemanticKernel;
using Soenneker.SemanticKernel.Dtos.Options;
using Soenneker.SemanticKernel.Enums.KernelType;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool.Abstract;

/// <summary>
/// Defines a collection of Semantic Kernel entries organized into named sub-pools, with cached kernel construction and per-entry quotas.
/// </summary>
public interface ISemanticKernelPool
{
    /// <summary>
    /// Gets the first entry, in insertion order, whose type matches and whose configured quota can be consumed.
    /// If <paramref name="type"/> is null, <see cref="KernelType.Chat"/> is used. When no entry is available, retries every 500 ms.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="type">Optional desired kernel type; use <see cref="KernelType.Chat"/> by default.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A tuple containing the cached <see cref="Kernel"/> and the entry whose quota was consumed.
    /// </returns>
    ValueTask<(Kernel? kernel, IKernelPoolEntry? entry)> GetAvailable(string poolId, KernelType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the remaining usage quotas for every entry in the specified pool.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a <see cref="Dictionary{TKey, TValue}"/>,
    /// where each key is an entryKey and the value is a tuple of
    /// (secondsRemaining, minutesRemaining, daysRemaining).
    /// </returns>
    ValueTask<Dictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(string poolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new kernel entry to the specified sub-pool. Entry keys should be unique across all sub-pools because the shared kernel cache is keyed only by entry key.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Unique key for this kernel entry.</param>
    /// <param name="options"><see cref="SemanticKernelOptions"/> must have <see cref="SemanticKernelOptions.Type"/> set.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the add operation is complete.</returns>
    ValueTask Add(string poolId, string entryKey, SemanticKernelOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an existing <see cref="IKernelPoolEntry"/> under the specified poolId.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Unique key for this kernel entry.</param>
    /// <param name="entry">Pre-constructed <see cref="IKernelPoolEntry"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the add operation is complete.</returns>
    ValueTask Add(string poolId, string entryKey, IKernelPoolEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters (removes) the entry with <paramref name="entryKey"/> from the specified pool.
    /// Also removes that entry from the internal cache.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Key of the kernel entry to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{Boolean}"/> that is true if the entry existed and was removed;
    /// false if it was not present.
    /// </returns>
    ValueTask<bool> Remove(string poolId, string entryKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified sub-pool and clears the entire shared kernel cache, including kernels created for other sub-pools.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the Semantic Kernel Pool has been cleared.</returns>
    ValueTask Clear(string poolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears and removes every sub-pool (all poolIds) and clears the internal cache completely.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the Semantic Kernel Pool has been cleared.</returns>
    ValueTask ClearAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to fetch the <see cref="IKernelPoolEntry"/> for a given poolId and entryKey without modifying state.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Key of the kernel entry to look up.</param>
    /// <param name="entry">
    /// When this method returns, contains the <see cref="IKernelPoolEntry"/> if found; otherwise null.
    /// </param>
    /// <returns>True if the entry was found; otherwise false.</returns>
    bool TryGet(string poolId, string entryKey, out IKernelPoolEntry? entry);
}
