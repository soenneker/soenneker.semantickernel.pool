using Soenneker.SemanticKernel.Dtos.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool.Abstract;

/// <summary>
/// Represents a single kernel source (model + API key) with rate limiting capabilities.
/// </summary>
public interface IKernelPoolEntry
{
    public IKernelRateLimiter RateLimiter { get; }

    public SemanticKernelOptions Options { get; }

    public string Key { get; }

    /// <summary>
    /// Gets whether this kernel is currently available based on rate limits.
    /// </summary>
    ValueTask<bool> IsAvailable(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining quota for this kernel entry.
    /// </summary>
    ValueTask<(int Second, int Minute, int Day)> RemainingQuota(CancellationToken cancellationToken = default);
} 