using Microsoft.SemanticKernel;
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
using Soenneker.SemanticKernel.Enums.KernelType;
using Soenneker.Utils.Delay;

namespace Soenneker.SemanticKernel.Pool;

///<inheritdoc cref="ISemanticKernelPool"/>
public sealed class SemanticKernelPool : ISemanticKernelPool
{
    private readonly ConcurrentDictionary<string, SubPool> _subPools = new();
    private readonly ISemanticKernelCache _kernelCache;

    public SemanticKernelPool(ISemanticKernelCache kernelCache)
    {
        _kernelCache = kernelCache;
    }

    public async ValueTask<(Kernel? kernel, IKernelPoolEntry? entry)> GetAvailable(string poolId, KernelType? type = null,
        CancellationToken cancellationToken = default)
    {
        if (type == null)
        {
            type = KernelType.Chat;
        }

        SubPool pool = _subPools.GetOrAdd(poolId, _ => new SubPool());

        while (!cancellationToken.IsCancellationRequested)
        {
            List<(string Key, IKernelPoolEntry Entry)> candidates;

            using (await pool.QueueLock.Lock(cancellationToken).NoSync())
            {
                candidates = new List<(string, IKernelPoolEntry)>(pool.Entries.Count);

                foreach (string key in pool.OrderedKeys)
                {
                    if (!pool.Entries.TryGetValue(key, out IKernelPoolEntry? entry))
                        continue;

                    if (entry.Options.Type != type)
                        continue;

                    candidates.Add((key, entry));
                }
            }

            foreach ((string key, IKernelPoolEntry entry) in candidates)
            {
                if (!await entry.IsAvailable(cancellationToken).NoSync())
                    continue;

                if (!pool.Entries.TryGetValue(key, out IKernelPoolEntry? stillLive) || stillLive.Options.Type != type)
                {
                    continue;
                }

                Kernel kernel = await _kernelCache.Get(key, entry.Options, cancellationToken).NoSync();
                return (kernel, entry);
            }

            await DelayUtil.Delay(TimeSpan.FromMilliseconds(500), null, cancellationToken).NoSync();
        }

        return (null, null);
    }

    public async ValueTask<Dictionary<string, (int Second, int Minute, int Day)>> GetRemainingQuotas(string poolId,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, (int, int, int)>();

        if (!_subPools.TryGetValue(poolId, out SubPool? pool))
            return result;

        foreach (KeyValuePair<string, IKernelPoolEntry> kvp in pool.Entries)
        {
            (int Second, int Minute, int Day) quota = await kvp.Value.RemainingQuota(cancellationToken).NoSync();
            result[kvp.Key] = quota;
        }

        return result;
    }

    public async ValueTask Add(string poolId, string entryKey, SemanticKernelOptions options, CancellationToken cancellationToken = default)
    {
        if (options.Type == null)
            throw new ArgumentException("Type must be set on SemanticKernelOptions", nameof(options));

        var entry = new KernelPoolEntry(entryKey, options);
        await Add(poolId, entryKey, entry, cancellationToken);
    }

    public async ValueTask Add(string poolId, string entryKey, IKernelPoolEntry entry, CancellationToken cancellationToken = default)
    {
        SubPool pool = _subPools.GetOrAdd(poolId, _ => new SubPool());

        if (pool.Entries.TryAdd(entryKey, entry))
        {
            using (await pool.QueueLock.Lock(cancellationToken).NoSync())
            {
                LinkedListNode<string> node = pool.OrderedKeys.AddLast(entryKey);
                pool.NodeMap[entryKey] = node;
            }
        }
    }

    public async ValueTask<bool> Remove(string poolId, string entryKey, CancellationToken cancellationToken = default)
    {
        if (!_subPools.TryGetValue(poolId, out SubPool? pool))
            return false;

        if (!pool.Entries.TryRemove(entryKey, out _))
            return false;

        using (await pool.QueueLock.Lock(cancellationToken).NoSync())
        {
            if (pool.NodeMap.TryGetValue(entryKey, out LinkedListNode<string>? node))
            {
                pool.OrderedKeys.Remove(node);
                pool.NodeMap.Remove(entryKey);
            }
        }

        await _kernelCache.Remove(entryKey, cancellationToken).NoSync();
        return true;
    }

    public async ValueTask Clear(string poolId, CancellationToken cancellationToken = default)
    {
        if (!_subPools.TryRemove(poolId, out SubPool? pool))
            return;

        pool.Entries.Clear();

        using (await pool.QueueLock.Lock(cancellationToken).NoSync())
        {
            pool.OrderedKeys = [];
            pool.NodeMap.Clear();
        }

        await _kernelCache.Clear(cancellationToken).NoSync();
    }

    public async ValueTask ClearAll(CancellationToken cancellationToken = default)
    {
        _subPools.Clear();
        await _kernelCache.Clear(cancellationToken).NoSync();
    }

    public bool TryGet(string poolId, string entryKey, out IKernelPoolEntry? entry)
    {
        entry = null;
        
        if (!_subPools.TryGetValue(poolId, out SubPool? pool))
            return false;

        return pool.Entries.TryGetValue(entryKey, out entry);
    }
}