[![](https://img.shields.io/nuget/v/soenneker.semantickernel.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.semantickernel.pool/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.semantickernel.pool/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.semantickernel.pool/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.semantickernel.pool.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.semantickernel.pool/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.SemanticKernel.Pool

Manages a pool of Semantic Kernel instances with per-entry rate limiting.

## Features

- Thread-safe kernel pool management
- Per-entry rate limiting with sliding windows
- Support for any type of Semantic Kernel model

## Installation

```bash
dotnet add package Soenneker.SemanticKernel.Pool
```