using Microsoft.SemanticKernel;
using Soenneker.Extensions.ValueTask;
using Soenneker.SemanticKernel.Cache.Abstract;
using Soenneker.SemanticKernel.Pool.Abstract;
using Nito.AsyncEx;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool;

///<inheritdoc cref="IKernelPoolManager"/>
public sealed class KernelPoolManager : IKernelPoolManager
{
    private readonly ConcurrentDictionary<string, IKernelPoolEntry> _entries = new();
    private readonly ConcurrentQueue<string> _orderedKeys = new();
    private readonly AsyncLock _queueLock = new();

    private readonly ISemanticKernelCache _kernelCache;

    public KernelPoolManager(ISemanticKernelCache kernelCache)
    {
        _kernelCache = kernelCache;
    }

    public async ValueTask<(Kernel kernel, IKernelPoolEntry entry)?> GetAvailableKernel(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (string key in _orderedKeys)
            {
                if (_entries.TryGetValue(key, out IKernelPoolEntry? entry))
                {
                    if (await entry.IsAvailable(cancellationToken))
                    {
                        Kernel kernel = await _kernelCache.Get(key, entry.Options, cancellationToken).NoSync();
                        return (kernel, entry);
                    }
                }
            }

            await Task.Delay(100, cancellationToken);
        }

        return null;
    }

    public async ValueTask<ConcurrentDictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(CancellationToken cancellationToken = default)
    {
        var result = new ConcurrentDictionary<string, (int Second, int Minute, int Day)>();

        foreach ((string key, IKernelPoolEntry entry) in _entries)
        {
            (int Second, int Minute, int Day) quota = await entry.RemainingQuota(cancellationToken).NoSync();
            result.TryAdd(key, quota);
        }

        return result;
    }

    public async ValueTask Register(string key, IKernelPoolEntry entry, CancellationToken cancellationToken = default)
    {
        if (_entries.TryAdd(key, entry))
        {
            using (await _queueLock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                _orderedKeys.Enqueue(key);
            }
        }
    }

    public async ValueTask<bool> Unregister(string key, CancellationToken cancellationToken = default)
    {
        bool removed = _entries.TryRemove(key, out _);

        if (removed)
        {
            using (await _queueLock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                IEnumerable<string> keys = _orderedKeys.ToArray().Where(k => k != key);

                _orderedKeys.Clear();

                foreach (string k in keys)
                {
                    _orderedKeys.Enqueue(k);
                }
            }

            await _kernelCache.Remove(key);
        }

        return removed;
    }

    public bool TryGet(string key, out IKernelPoolEntry? entry)
    {
        return _entries.TryGetValue(key, out entry);
    }
}
