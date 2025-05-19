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
- **Thread Safety**: Fully thread-safe implementation using concurrent collections
- **Async Support**: Modern async/await patterns throughout the codebase
- **Flexible Configuration**: Configurable rate limits and pool settings
- **Resource Management**: Automatic cleanup of expired rate limit windows

## Installation

```bash
dotnet add package Soenneker.SemanticKernel.Pool
```

```csharp
services.AddSemanticKernelPoolAsSingleton()
```

## Extension Packages

This library has several extension packages for different AI providers:

- [Soenneker.SemanticKernel.Pool.Gemini](https://www.nuget.org/packages/Soenneker.SemanticKernel.Pool.Gemini/) - Google Gemini integration
- [Soenneker.SemanticKernel.Pool.OpenAi](https://www.nuget.org/packages/Soenneker.SemanticKernel.Pool.OpenAi/) - OpenAI/OpenRouter.ai/etc integration
- [Soenneker.SemanticKernel.Pool.Ollama](https://www.nuget.org/packages/Soenneker.SemanticKernel.Pool.Ollama/) - Ollama integration
- [Soenneker.SemanticKernel.Pool.OpenAi.Azure](https://www.nuget.org/packages/Soenneker.SemanticKernel.Pool.OpenAi.Azure/) - Azure OpenAI integration

## Usage

### Startup Configuration

```csharp
// In Program.cs or Startup.cs
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add the kernel pool as a singleton
        builder.Services.AddSemanticKernelPoolAsSingleton();

        var app = builder.Build();

        // Register kernels during startup
        var kernelPool = app.Services.GetRequiredService<ISemanticKernelPool>();
        
        // Manually create options, or use one of the extensions mentioned above
        var options = new SemanticKernelOptions
        {
            ApiKey = "your-api-key",
            Endpoint = "https://api.openai.com/v1",
            Model = "gpt-4",
            KernelFactory = async (opts, _) =>
            {
                return Kernel.CreateBuilder()
                             .AddOpenAIChatCompletion(modelId: opts.ModelId!,
                                 new OpenAIClient(new ApiKeyCredential(opts.ApiKey), new OpenAIClientOptions {Endpoint = new Uri(opts.Endpoint)}));
            }

            // Rate Limiting
            RequestsPerSecond = 10,
            RequestsPerMinute = 100,
            RequestsPerDay = 1000,
            TokensPerDay = 10000
        };

        await kernelPool.Register("my-kernel", options);

        // Add more registrations... order matters!

        await app.RunAsync();
    }
}
```

### Using the Pool

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
        // Get an available kernel that's within its rate limits, preferring the first registered
        var (kernel, entry) = await _kernelPool.GetAvailableKernel();

        // Get the chat completion service
        var chatCompletionService = kernel.GetService<IChatCompletionService>();

        // Create a chat history
        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.User, "What is the capital of France?");

        // Execute chat completion
        var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory);

        Console.WriteLine($"Response: {response.Content}");

        // Access rate limit information through the entry
        var remainingQuota = await entry.RemainingQuota();
        Console.WriteLine($"Remaining requests - Second: {remainingQuota.Second}, Minute: {remainingQuota.Minute}, Day: {remainingQuota.Day}");
    }
}
```
