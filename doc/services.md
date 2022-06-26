---
uid: services
---

# Services

The <xref:ZeroInstall.Services> namespace provides services for solving dependencies, downloading implementations, executing apps, etc..

## Dependency injection

The <xref:ZeroInstall.Services.ServiceProvider> class provides instances of various services. You can think of it as a hard-coded dependency injection container. We use this instead of a runtime DI system to avoid the performance impact of reflection, keeping the cold-start time short. This is important so that starting a cached program with `0install run` does not add a significant overhead when compared to launching it directly.

To instantiate the service provider you need to provide the constructor with an <xref:NanoByte.Common.Tasks.ITaskHandler>. You should use exactly one instance of the service provider per user request to ensure consistent state during execution. Rather than instantiating the service provider class, another pattern used in the Zero Install code-base is to inherit from it.

You can also use the [.AddZeroInstall()](xref:ZeroInstall.Services.ServiceCollectionExtensions) extension method for `IServiceCollection` to replace the service provider with [.NET's built-in DI framework](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection).

## Sample use-case

A simplified version of the `0install run` logic can be implemented using the Zero Install services as follows:

1. Pass <xref:ZeroInstall.Model.Requirements> to [ISolver.Solve()](xref:ZeroInstall.Services.Solvers.ISolver) and get <xref:ZeroInstall.Model.Selection.Selections>.
1. Pass <xref:ZeroInstall.Model.Selection.Selections> to [ISelectionsManager.GetUncachedImplementations()](xref:ZeroInstall.Services.SelectionsManagerExtensions#ZeroInstall_Services_SelectionsManagerExtensions_GetUncachedImplementations_ZeroInstall_Services_ISelectionsManager_ZeroInstall_Model_Selection_Selections_) and get uncached <xref:ZeroInstall.Model.Implementation>.
1. Pass <xref:ZeroInstall.Model.Implementation> to [IFetcher.Fetch()](xref:ZeroInstall.Services.Fetchers.IFetcher).
v Pass <xref:ZeroInstall.Model.Selection.Selections> to [IExecutor.Start()](xref:ZeroInstall.Services.Executors.IExecutor#ZeroInstall_Services_Executors_IExecutor_Start_ZeroInstall_Model_Selection_Selections_).

Sample code for implementing this in various languages:

- [C#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.cs)
- [Visual Basic .NET](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.vb)
- [F#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.fs)
- [IronPython](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.py)
