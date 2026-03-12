using Soenneker.SemanticKernel.Dtos.Options;
using Soenneker.SemanticKernel.Pool.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool;

///<inheritdoc cref="IKernelPoolEntry"/>
public sealed class KernelPoolEntry : IKernelPoolEntry
{
    public IKernelRateLimiter RateLimiter { get; }

    public SemanticKernelOptions Options { get; }

    public string Key { get; }

    public KernelPoolEntry(string key, SemanticKernelOptions options)
    {
        Key = key;
        Options = options;
        RateLimiter = new KernelRateLimiter(options);
    }

    public ValueTask<bool> IsAvailable(CancellationToken cancellationToken = default)
    {
        return RateLimiter.TryConsume(cancellationToken);
    }

    public ValueTask<(int Second, int Minute, int Day)> RemainingQuota(CancellationToken cancellationToken = default)
    {
        return RateLimiter.GetRemaining(cancellationToken);
    }
}