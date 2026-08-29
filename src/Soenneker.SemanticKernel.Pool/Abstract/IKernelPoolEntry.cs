using Soenneker.SemanticKernel.Dtos.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool.Abstract;

/// <summary>
/// Represents a single kernel source (model + API key) with rate limiting capabilities.
/// </summary>
public interface IKernelPoolEntry
{
    /// <summary>
    /// Gets rate limiter.
    /// </summary>
    public IKernelRateLimiter RateLimiter { get; }

    /// <summary>
    /// Gets options.
    /// </summary>
    public SemanticKernelOptions Options { get; }

    /// <summary>
    /// Gets key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets whether this kernel is currently available based on rate limits.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if gets whether this kernel is currently available based on rate limits; otherwise, false.</returns>
    ValueTask<bool> IsAvailable(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining quota for this kernel entry.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested (int Second, int Minute, int Day).</returns>
    ValueTask<(int Second, int Minute, int Day)> RemainingQuota(CancellationToken cancellationToken = default);
}
