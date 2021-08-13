Zero Install is a decentralized cross-platform software installation system. You can learn more at [0install.net](https://0install.net/).  
This website documents the Zero Install .NET API. You can use this to integrate Zero Install features into your own application.

[**GitHub repository**](https://github.com/0install/0install-dotnet)

**Sample code**

- [C#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.cs)
- [Visual Basic .NET](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.vb)
- [F#](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.fs)
- [IronPython](https://github.com/0install/dotnet-backend/blob/master/samples/MinimalZeroInstall.py)

**NuGet packages**

[ZeroInstall.Model](https://www.nuget.org/packages/ZeroInstall.Model/)  
Data model for the [feed format](https://docs.0install.net/specifications/feed/).

[ZeroInstall.Store](https://www.nuget.org/packages/ZeroInstall.Store/)  
Management of [implementation caches](https://docs.0install.net/details/cache/), [digital signatures](https://docs.0install.net/specifications/feed/#digital-signatures), etc..

[ZeroInstall.Archives](https://www.nuget.org/packages/ZeroInstall.Archives/)  
Extracting and building archives (`.zip`, `.tar`, etc.).

[ZeroInstall.Services](https://www.nuget.org/packages/ZeroInstall.Services/)  
Core services like [solving dependencies](https://docs.0install.net/developers/solver/), downloading implementations and execution selections.  
Zero Install itself is built upon this API. You can use the API to integrate Zero Install features into your own application.

[ZeroInstall.DesktopIntegration](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)  
Integrating applications with desktop environments (creating menu entries, etc.).

[ZeroInstall.Commands](https://www.nuget.org/packages/ZeroInstall.Commands/)  
Command-line interface for Zero Install.  
The binary in this package serves both as an actual CLI and a library for building other clients.

[ZeroInstall.Publish](https://www.nuget.org/packages/ZeroInstall.Publish/)  
Utilities for creating and modifying feed files.  
The [Zero Install Publishing Tools](https://github.com/0install/0publish-win) (including the Feed Editor) are built upon this library. You can use this to automate complex feed creation/update tasks.

The following graph shows the dependencies between the NuGet packages:

\image html nuget-dependencies.svg
