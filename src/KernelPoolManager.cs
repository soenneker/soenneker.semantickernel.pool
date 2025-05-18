using Microsoft.SemanticKernel;
using Soenneker.Extensions.ValueTask;
using Soenneker.SemanticKernel.Cache.Abstract;
using Soenneker.SemanticKernel.Pool.Abstract;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool;

///<inheritdoc cref="IKernelPoolManager"/>
public sealed class KernelPoolManager : IKernelPoolManager
{
    private readonly ConcurrentDictionary<string, IKernelPoolEntry> _entries = new();
    private readonly ISemanticKernelCache _kernelCache;

    public KernelPoolManager(ISemanticKernelCache kernelCache)
    {
        _kernelCache = kernelCache;
    }

    public async ValueTask<(Kernel kernel, IKernelPoolEntry entry)?> GetAvailableKernel(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach ((string key, IKernelPoolEntry entry) in _entries)
            {
                if (await entry.IsAvailable(cancellationToken))
                {
                    Kernel kernel = await _kernelCache.Get(key, entry.Options, cancellationToken).NoSync();
                    return (kernel, entry);
                }
            }

            await Task.Delay(100, cancellationToken);
        }

        return null;
    }

    public ValueTask<ConcurrentDictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(CancellationToken cancellationToken = default)
    {
        var result = new ConcurrentDictionary<string, (int Second, int Minute, int Day)>();

        foreach ((string key, IKernelPoolEntry entry) in _entries)
        {
            (int Second, int Minute, int Day) quota = entry.RemainingQuota(cancellationToken).AwaitSync(); // sync safe in this context
            result.TryAdd(key, quota);
        }

        return ValueTask.FromResult(result);
    }

    public void Register(string key, IKernelPoolEntry entry)
    {
        _entries[key] = entry;
    }

    public bool TryGet(string key, out IKernelPoolEntry? entry)
    {
        return _entries.TryGetValue(key, out entry);
    }
}
