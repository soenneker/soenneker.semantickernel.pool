﻿using Nito.AsyncEx;
using Soenneker.SemanticKernel.Pool.Abstract;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Soenneker.SemanticKernel.Pool;

public sealed class SubPool
{
    public readonly ConcurrentDictionary<string, IKernelPoolEntry> Entries = new();
    public LinkedList<string> OrderedKeys = new();
    public readonly Dictionary<string, LinkedListNode<string>> NodeMap = new();
    public readonly AsyncLock QueueLock = new();
}