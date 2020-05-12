Zero Install is a decentralized cross-platform software installation system. You can learn more at [0install.net](http://0install.net/).  
This website documents the Zero Install .NET API. You can use this to integrate Zero Install features into your own application.

Take a look at the [Namespace List](namespaces.html) to get an overview of the available functionality.  
See the [GitHub repository](https://github.com/TypedRest/TypedRest-DotNet) to report issues, contribute code, etc..

**NuGet packages**

[ZeroInstall.Store](https://www.nuget.org/packages/ZeroInstall.Store/)  
Data model for the [feed format](https://docs.0install.de/specifications/feed/), signature verification, management of [on-disk caches](https://docs.0install.de/details/cache/).  
This provides a common basis for the packages `ZeroInstall.Services` and `ZeroInstall.Publish`. You will usually get this package indirectly as a dependency from there.

[ZeroInstall.Services](https://www.nuget.org/packages/ZeroInstall.Services/)  
Core services like [solving dependencies](https://docs.0install.de/developers/solver/), downloading implementations and execution selections.  
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
