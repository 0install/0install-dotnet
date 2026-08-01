# ZeroInstall.Services

Core Zero Install services for solving dependencies, downloading implementations, executing apps, etc.

[Zero Install](https://0install.net/) is a decentralized cross-platform software installation system. This package is part of the [.NET implementation](https://github.com/0install/0install-dotnet) of Zero Install. Zero Install itself is built upon this API. You can use the API to integrate Zero Install features into your own application.

## Dependency injection

The `ServiceProvider` class provides instances of various services. You can think of it as a hard-coded dependency injection container. We use this instead of a runtime DI system to avoid the performance impact of reflection, keeping the cold-start time short. This is important so that starting a cached program with `0install run` does not add a significant overhead when compared to launching it directly.

To instantiate the service provider you need to provide the constructor with an `ITaskHandler`. You should use exactly one instance of the service provider per user request to ensure consistent state during execution. Rather than instantiating the service provider class, another pattern used in the Zero Install code-base is to inherit from it.

You can also use the `.AddZeroInstall()` extension method for `IServiceCollection` to replace the service provider with [.NET's built-in DI framework](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection).

## Sample use-case

A simplified version of the `0install run` logic can be implemented using the Zero Install services as follows:

1. Pass `Requirements` to `ISolver.Solve()` and get `Selections`.
1. Pass `Selections` to `ISelectionsManager.GetUncachedImplementations()` and get uncached `Implementation`s.
1. Pass `Implementation`s to `IFetcher.Fetch()`.
1. Pass `Selections` to `IExecutor.Start()`.

Sample code for implementing this in various languages:

- [C#](https://github.com/0install/0install-dotnet/blob/master/samples/MinimalZeroInstall.cs)
- [Visual Basic .NET](https://github.com/0install/0install-dotnet/blob/master/samples/MinimalZeroInstall.vb)
- [F#](https://github.com/0install/0install-dotnet/blob/master/samples/MinimalZeroInstall.fs)
- [IronPython](https://github.com/0install/0install-dotnet/blob/master/samples/MinimalZeroInstall.py)
