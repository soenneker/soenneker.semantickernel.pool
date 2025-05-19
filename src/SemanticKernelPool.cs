using Microsoft.SemanticKernel;
using Nito.AsyncEx;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.SemanticKernel.Cache.Abstract;
using Soenneker.SemanticKernel.Dtos.Options;
using Soenneker.SemanticKernel.Pool.Abstract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SemanticKernel.Pool;

///<inheritdoc cref="ISemanticKernelPool"/>
public sealed class SemanticKernelPool : ISemanticKernelPool
{
    private readonly ConcurrentDictionary<string, IKernelPoolEntry> _entries = new();
    private readonly ConcurrentQueue<string> _orderedKeys = new();
    private readonly AsyncLock _queueLock = new();

    private readonly ISemanticKernelCache _kernelCache;

    public SemanticKernelPool(ISemanticKernelCache kernelCache)
    {
        _kernelCache = kernelCache;
    }

    public async ValueTask<(Kernel? kernel, IKernelPoolEntry? entry)> GetAvailableKernel(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using (await _queueLock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (string key in _orderedKeys)
                {
                    if (_entries.TryGetValue(key, out IKernelPoolEntry? entry))
                    {
                        if (await entry.IsAvailable(cancellationToken).NoSync())
                        {
                            Kernel kernel = await _kernelCache.Get(key, entry.Options, cancellationToken).NoSync();
                            return (kernel, entry);
                        }
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(500), cancellationToken).NoSync();
        }

        return (null, null);
    }

    public async ValueTask<ConcurrentDictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(CancellationToken cancellationToken = default)
    {
        var result = new ConcurrentDictionary<string, (int, int, int)>();

        foreach (KeyValuePair<string, IKernelPoolEntry> kvp in _entries)
        {
            (int Second, int Minute, int Day) quota = await kvp.Value.RemainingQuota(cancellationToken).NoSync();
            result.TryAdd(kvp.Key, quota);
        }

        return result;
    }

    public ValueTask Register(string key, SemanticKernelOptions options, CancellationToken cancellationToken = default)
    {
        var entry = new KernelPoolEntry(key, options);
        return Register(key, entry, cancellationToken);
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
                int count = _orderedKeys.Count;

                for (var i = 0; i < count; i++)
                {
                    if (_orderedKeys.TryDequeue(out string? k) && k != key)
                        _orderedKeys.Enqueue(k);
                }
            }

            await _kernelCache.Remove(key, cancellationToken).NoSync();
        }

        return removed;
    }

    public bool TryGet(string key, out IKernelPoolEntry? entry)
    {
        return _entries.TryGetValue(key, out entry);
    }
}