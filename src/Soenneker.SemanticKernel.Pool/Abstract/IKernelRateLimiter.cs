using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool.Abstract;

/// <summary>
/// Represents a rate limiter that tracks requests per second, per minute, and per day using sliding windows.
/// </summary>
public interface IKernelRateLimiter
{
    /// <summary>
    /// Attempts to consume a token from the rate limiter.
    /// </summary>
    /// <returns>True if a token was consumed successfully, false if the rate limit was exceeded.</returns>
    ValueTask<bool> TryConsume(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining quota for each time window.
    /// </summary>
    /// <returns>A tuple containing the remaining requests for second, minute, and day windows.</returns>
    ValueTask<(int Second, int Minute, int Day)> GetRemaining(CancellationToken cancellationToken = default);
}