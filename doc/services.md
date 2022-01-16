---
uid: services
---

# Services

The <xref:ZeroInstall.Services> namespace provides services for solving dependencies, downloading implementations, executing apps, etc..

## Dependency injection

The <xref:ZeroInstall.Services.ServiceProvider> class provides instances of all important services. You can think of it as a hard-coded dependency injection container. We use this instead of a runtime DI system to avoid the performance overhead of reflection. This is important for Zero Install to keep the cold-start time as short as possible (e.g., to avoid `0install run` for an already cached app from taking longer than necessary).

To instantiate the service provider you need to provide the constructor with an <xref:NanoByte.Common.Tasks.ITaskHandler>. You should use exactly one instance of the service provider per user request to ensure consistent state during execution. Rather than instantiating the service provider class, another pattern used in the Zero Install code-base is to inherit from it.

You can also use the [.AddZeroInstall()](xref:ZeroInstall.Services.ServiceCollectionExtensions) extension method for `IServiceCollection` to replace the service provider with [.NET's built-in DI framework](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection).

## Use-case

A strongly simplified version of the `0install run` logic could use the services provided by the service provider as follows:

- Pass <xref:ZeroInstall.Model.Requirements> to [ISolver.Solve()](xref:ZeroInstall.Services.Solvers.ISolver) and get <xref:ZeroInstall.Model.Selection.Selections>.
- Pass <xref:ZeroInstall.Model.Selection.Selections> to [ISelectionsManager.GetUncachedImplementations()](xref:ZeroInstall.Services.SelectionsManagerExtensions#ZeroInstall_Services_SelectionsManagerExtensions_GetUncachedImplementations_ZeroInstall_Services_ISelectionsManager_ZeroInstall_Model_Selection_Selections_) and get uncached <xref:ZeroInstall.Model.Implementation>.
- Pass <xref:ZeroInstall.Model.Implementation> to [IFetcher.Fetch()](xref:ZeroInstall.Services.Fetchers.IFetcher).
- Pass <xref:ZeroInstall.Model.Selection.Selections> to [IExecutor.Start()](xref:ZeroInstall.Services.Executors.IExecutor#ZeroInstall_Services_Executors_IExecutor_Start_ZeroInstall_Model_Selection_Selections_).

## Sample code

- [C#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.cs)
- [Visual Basic .NET](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.vb)
- [F#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.fs)
- [IronPython](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.py)
