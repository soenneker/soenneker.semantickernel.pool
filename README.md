[![](https://img.shields.io/nuget/v/soenneker.semantickernel.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.semantickernel.pool/)  
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.semantickernel.pool/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.semantickernel.pool/actions/workflows/publish-package.yml)  
[![](https://img.shields.io/nuget/dt/soenneker.semantickernel.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.semantickernel.pool/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.SemanticKernel.Pool

A high-performance, thread-safe pool implementation for Microsoft Semantic Kernel instances with built-in rate limiting capabilities.

## Features

- **Kernel Pooling**: Efficiently manages and reuses Semantic Kernel instances  
- **Rate Limiting**: Built-in support for request rate limiting at multiple time windows:  
  - Per-second rate limiting  
  - Per-minute rate limiting  
  - Per-day rate limiting  
  - Token-based rate limiting  
- **Thread Safety**: Fully thread-safe implementation using concurrent collections and async locking  
- **Flexible Configuration**: Configurable rate limits and pool settings  
- **Resource Management**: Automatic cleanup of expired rate limit windows  

## Installation

```bash
dotnet add package Soenneker.SemanticKernel.Pool
````

```csharp
services.AddSemanticKernelPoolAsSingleton();
```

## Extension Packages

This library has several extension packages for different AI providers:

* [Soenneker.SemanticKernel.Pool.Gemini](https://github.com/soenneker/Soenneker.SemanticKernel.Pool.Gemini/) – Google Gemini integration
* [Soenneker.SemanticKernel.Pool.OpenAi](https://github.com/soenneker/Soenneker.SemanticKernel.Pool.OpenAi/) – OpenAI/OpenRouter.ai/etc. integration
* [Soenneker.SemanticKernel.Pool.Ollama](https://github.com/soenneker/Soenneker.SemanticKernel.Pool.Ollama/) – Ollama integration
* [Soenneker.SemanticKernel.Pool.OpenAi.Azure](https://github.com/soenneker/Soenneker.SemanticKernel.Pool.OpenAi.Azure/) – Azure OpenAI integration

## Usage

### Startup Configuration

```csharp
// Program.cs or Startup.cs
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add the kernel pool as a singleton
        builder.Services.AddSemanticKernelPoolAsSingleton();

        var app = builder.Build();

        // Get the pool service
        var kernelPool = app.Services.GetRequiredService<ISemanticKernelPool>();

        // Create SemanticKernelOptions (example uses OpenAI)
        var options = new SemanticKernelOptions
        {
            ApiKey = "your-api-key",
            Endpoint = "https://api.openai.com/v1",
            Model = "gpt-4",
            KernelFactory = async (opts, _) =>
            {
                return Kernel.CreateBuilder()
                             .AddOpenAIChatCompletion(
                                 modelId: opts.ModelId!,
                                 new OpenAIClient(
                                     new ApiKeyCredential(opts.ApiKey),
                                     new OpenAIClientOptions { Endpoint = new Uri(opts.Endpoint) }));
            },

            // Rate Limiting
            RequestsPerSecond = 10,
            RequestsPerMinute = 100,
            RequestsPerDay = 1000,
            TokensPerDay = 10000
        };

        // Register one or more entries under a "sub-pool-1" sub-pool
        // poolId: "sub-pool-1", entryKey: "entry1"
        await kernelPool.Register("sub-pool-1", "entry1", options);

        // You can register additional entries (with different entryKey or options)
        // await kernelPool.Register("sub-pool-1", "entry2", otherOptions);

        await app.RunAsync();
    }
}
```

### Working with Sub-Pools

Each call to `Register(...)` creates a new “entry” under the specified `poolId`. Entries are checked out in the order they were added (round-robin), subject to rate limits. You can have multiple sub-pools by choosing different `poolId` strings:

```csharp
// Register two separate sub-pools
await kernelPool.Register("reasoning", "o4-mini-high", reasoningOptions);
await kernelPool.Register("high-performance", "4o", highPerformanceOptions);
```

### Retrieving an Available Kernel

```csharp
public class MyService
{
    private readonly ISemanticKernelPool _kernelPool;

    public MyService(ISemanticKernelPool kernelPool)
    {
        _kernelPool = kernelPool;
    }

    public async Task ProcessAsync()
    {
        // Attempt to get an available kernel from the "my-kernel" sub-pool
        // If no type is provided, defaults to KernelType.Chat
        var (kernel, entry) = await _kernelPool.GetAvailableKernel("my-kernel");

        if (kernel is null || entry is null)
        {
            Console.WriteLine("No available kernel or operation was cancelled.");
            return;
        }

        // Use the kernel as usual
        var chatCompletion = kernel.GetService<IChatCompletionService>();

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.User, "What's the capital of France?");

        var response = await chatCompletion.GetChatMessageContentAsync(chatHistory);
        Console.WriteLine($"Response: {response.Content}");

        // Check rate limit usage
        var remaining = await entry.RemainingQuota();
        Console.WriteLine($"Remaining quotas — Second: {remaining.Second}, Minute: {remaining.Minute}, Day: {remaining.Day}");
    }
}
```

### Unregistering and Clearing

* **Remove a single entry from a sub-pool**

  ```csharp
  bool removed = await kernelPool.Remove("sub-pool-1", "entry1");
  ```

* **Clear a single sub-pool** (removes all entries under that `poolId` and clears cache for those entries)

  ```csharp
  await kernelPool.Clear("sub-pool-1");
  ```

* **Clear all sub-pools** (removes every `poolId`, all entries, and clears the entire cache)

  ```csharp
  await kernelPool.ClearAll();
  ```