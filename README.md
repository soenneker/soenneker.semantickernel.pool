[![](https://img.shields.io/nuget/v/soenneker.semantickernel.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.semantickernel.pool/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.semantickernel.pool/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.semantickernel.pool/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.semantickernel.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.semantickernel.pool/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.semantickernel.pool/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.semantickernel.pool/actions/workflows/codeql.yml)

# Soenneker.SemanticKernel.Pool

A keyed collection of Semantic Kernel configurations with per-entry request quotas and cached kernel construction.

## Installation

```bash
dotnet add package Soenneker.SemanticKernel.Pool
```

## Registration

```csharp
using Soenneker.SemanticKernel.Pool.Registrars;

services.AddSemanticKernelPoolAsSingleton();
```

`AddSemanticKernelPoolAsScoped()` is also available. Both registrations use the singleton Semantic Kernel cache, so cached kernels can survive disposal of a scoped pool.

## Add an entry

Each entry belongs to a named sub-pool and must specify its `KernelType`:

```csharp
using Microsoft.SemanticKernel;
using Soenneker.SemanticKernel.Dtos.Options;
using Soenneker.SemanticKernel.Enums.KernelType;
using Soenneker.SemanticKernel.Pool.Abstract;

var options = new SemanticKernelOptions
{
    Type = KernelType.Chat,
    ModelId = "primary-chat-model",
    RequestsPerSecond = 2,
    RequestsPerMinute = 60,
    RequestsPerDay = 1_000,
    KernelFactory = static (options, cancellationToken) =>
    {
        IKernelBuilder builder = Kernel.CreateBuilder();

        // Add the connector for options.ModelId, Endpoint, and ApiKey.

        return ValueTask.FromResult(builder);
    }
};

await pool.Add("chat", "primary", options, cancellationToken);
```

Provider-specific pool packages can create these options and connector registrations for OpenAI, Azure OpenAI, Gemini, Mistral, and Ollama.

## Acquire a kernel

```csharp
(Kernel? kernel, IKernelPoolEntry? entry) =
    await pool.GetAvailable("chat", KernelType.Chat, cancellationToken);

if (kernel is null || entry is null)
    return;

// Resolve and use the services configured by the entry's KernelFactory.
```

The pool checks matching entries in insertion order and returns the first entry whose quota can be consumed. It is not round-robin. If none is available, `GetAvailable` waits 500 ms and tries again until an entry becomes available or cancellation stops the operation.

Acquisition counts as one request against every configured request window. `TokensPerDay` also counts one unit per acquisition through this API; the pool does not inspect the provider response or actual model-token usage.

Inspect the remaining request quotas when needed:

```csharp
Dictionary<string, (int Second, int Minute, int Day)> quotas =
    await pool.GetRemainingQuotas("chat", cancellationToken);
```

An unconfigured quota is reported as `int.MaxValue`.

## Remove entries

```csharp
bool removed = await pool.Remove("chat", "primary", cancellationToken);
await pool.Clear("chat", cancellationToken);
await pool.ClearAll(cancellationToken);
```

Entry keys should be unique across all sub-pools. The underlying kernel cache is keyed by `entryKey`, not by the combination of `poolId` and `entryKey`.

`Remove` evicts the removed entry's cached kernel. `Clear(poolId)` removes that sub-pool and clears the shared kernel cache, including kernels cached for other sub-pools. `ClearAll` removes every sub-pool and clears the cache.
