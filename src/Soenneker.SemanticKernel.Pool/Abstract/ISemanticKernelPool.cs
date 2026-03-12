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
/// Defines a pool of SemanticKernel entries, organized by poolId.
/// Allows registering, unregistering, clearing, and checking out a kernel instance.
/// </summary>
public interface ISemanticKernelPool
{
    /// <summary>
    /// Attempts to fetch an available SemanticKernel from the specified pool.
    /// If <paramref name="type"/> is null, defaults to <see cref="KernelType.Chat"/>.
    /// Will retry every 500ms until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="type">Optional desired kernel type; use <see cref="KernelType.Chat"/> by default.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a tuple of:
    /// - <see cref="Kernel"/> instance (or null if cancelled)
    /// - Corresponding <see cref="IKernelPoolEntry"/> used to manage that kernel.
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
    /// Registers a new kernel entry under the specified poolId using <paramref name="options"/>.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Unique key for this kernel entry.</param>
    /// <param name="options">
    /// <see cref="SemanticKernelOptions"/> must have <see cref="SemanticKernelOptions.Type"/> set.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    ValueTask Add(string poolId, string entryKey, SemanticKernelOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an existing <see cref="IKernelPoolEntry"/> under the specified poolId.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="entryKey">Unique key for this kernel entry.</param>
    /// <param name="entry">Pre-constructed <see cref="IKernelPoolEntry"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
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
    /// Clears and removes all entries from the specified poolId, and also clears the internal cache.
    /// </summary>
    /// <param name="poolId">Identifier for the sub-pool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    ValueTask Clear(string poolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears and removes every sub-pool (all poolIds) and clears the internal cache completely.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
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