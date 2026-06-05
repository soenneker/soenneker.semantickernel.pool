using Soenneker.SemanticKernel.Pool.Abstract;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Soenneker.Asyncs.Locks;

namespace Soenneker.SemanticKernel.Pool;

/// <summary>
/// Represents the sub pool.
/// </summary>
public sealed class SubPool
{
    /// <summary>
    /// The entries.
    /// </summary>
    public readonly ConcurrentDictionary<string, IKernelPoolEntry> Entries = new();
    /// <summary>
    /// The ordered keys.
    /// </summary>
    public LinkedList<string> OrderedKeys = [];
    /// <summary>
    /// The node map.
    /// </summary>
    public readonly Dictionary<string, LinkedListNode<string>> NodeMap = new();
    /// <summary>
    /// The queue lock.
    /// </summary>
    public readonly AsyncLock QueueLock = new();
}