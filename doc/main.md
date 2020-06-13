Zero Install is a decentralized cross-platform software installation system. You can learn more at [0install.net](http://0install.net/).  
This website documents the Zero Install .NET API. You can use this to integrate Zero Install features into your own application.

Take a look at the [**Namespace List**](namespaces.html) to get an overview of the available functionality.

**NuGet packages**

[ZeroInstall.Model](https://www.nuget.org/packages/ZeroInstall.Model/)  
Data model for the [feed format](https://docs.0install.net/specifications/feed/).

[ZeroInstall.Store](https://www.nuget.org/packages/ZeroInstall.Store/)  
Management of [on-disk caches](https://docs.0install.net/details/cache/), [signature verification](https://docs.0install.net/specifications/feed/#digital-signatures), etc..  
This provides a common basis for the packages `ZeroInstall.Services` and `ZeroInstall.Publish`. You will usually get this package indirectly as a dependency from there.

[ZeroInstall.Services](https://www.nuget.org/packages/ZeroInstall.Services/)  
Core services like [solving dependencies](https://docs.0install.net/developers/solver/), downloading implementations and execution selections.  
Zero Install itself is built upon this API. You can use the API to integrate Zero Install features into your own application, e.g. for a plugin management system.

[ZeroInstall.Services.Interfaces](https://www.nuget.org/packages/ZeroInstall.Services.Interfaces/)  
Interfaces/abstractions for Zero Install services (contains no actual implementations).  
You will usually get this package indirectly as a dependency of `ZeroInstall.Services`.

[ZeroInstall.DesktopIntegration](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)  
Methods for integrating applications with desktop environments (creating menu entries, etc.).

[ZeroInstall.Commands](https://www.nuget.org/packages/ZeroInstall.Commands/)  
Command-line interface for Zero Install.  
The binary in this package serves both as an actual CLI and a library for building other clients.

[ZeroInstall.Publish](https://www.nuget.org/packages/ZeroInstall.Publish/)  
Utilities for creating and modifying feed files.  
The [Zero Install Publishing Tools](https://github.com/0install/0publish-win) (including the Feed Editor) are built upon this library. You can use this to automate complex feed creation/update tasks.

The following graph shows the dependencies between the NuGet packages:

\image html nuget-dependencies.svg

**Sample code**

- [C#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.cs)
- [Visual Basic .NET](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.vb)
- [F#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.fs)
- [IronPython](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.py)

[**GitHub repository**](https://github.com/0install/0install-dotnet)
