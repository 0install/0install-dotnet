# Services

The \ref ZeroInstall.Services namespace provides services for solving dependencies, downloading implementations, executing apps, etc..

### Dependency injection

The \ref ZeroInstall.Services.ServiceProvider "ServiceProvider" class provides instances of all important services. You can think of it as a hard-coded dependency injection container. We use this instead of a runtime DI system to avoid the performance overhead of reflection. This is important for Zero Install to keep the cold-start time as short as possible (e.g., to avoid `0install run` for an already cached app from taking longer than necessary).

To instantiate the service provider you need to provide the constructor with a [task handler](https://common.nano-byte.net/md_tasks.html). You should use exactly one instance of the service provider per user request to ensure consistent state during execution. Rather than instantiating the service provider class, another pattern used in the Zero Install code-base is to inherit from it.

You can also use the \ref ZeroInstall.Services.ServiceCollectionExtensions.AddZeroInstall ".AddZeroInstall()" extension method for `IServiceCollection` to replace the service provider with [.NET's built-in DI framework](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection).

### Use-case

A strongly simplified version of the `0install run` logic could use the services provided by the service provider as follows:

- Pass \ref ZeroInstall.Model.Requirements to \ref ZeroInstall.Services.Solvers.ISolver.Solve "ISolver.Solve()" and get \ref ZeroInstall.Model.Selection.Selections.
- Pass \ref ZeroInstall.Model.Selection.Selections to \ref ZeroInstall.Services.SelectionsManagerExtensions.GetUncachedImplementations "ISelectionsManager.GetUncachedImplementations()" and get uncached \ref ZeroInstall.Model.Implementation.
- Pass \ref ZeroInstall.Model.Implementation to \ref ZeroInstall.Services.Fetchers.IFetcher.Fetch "IFetcher.Fetch()".
- Pass \ref ZeroInstall.Model.Selection.Selections to \ref ZeroInstall.Services.Executors.IExecutor.Start "IExecutor.Start()".

### Sample code

- [C#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.cs)
- [Visual Basic .NET](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.vb)
- [F#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.fs)
- [IronPython](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.py)
