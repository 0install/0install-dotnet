# Zero Install .NET

This website documents the API provided by Zero Install .NET.

Zero Install is a decentralized cross-platform software installation system. You can learn more at [0install.net](http://0install.net/).

## Packages

You can use the following NuGet packages to integrate Zero Install features into your own application:

[ZeroInstall.Store](https://www.nuget.org/packages/ZeroInstall.Store/)  
Data model for the [feed format](https://docs.0install.de/specifications/feed/), signature verification, management of [on-disk caches](https://docs.0install.de/details/cache/)

[ZeroInstall.Services](https://www.nuget.org/packages/ZeroInstall.Services/)  
Core services like [solving dependencies](https://docs.0install.de/developers/solver/), downloading implementations and execution selections

[ZeroInstall.Services.Interfaces](https://www.nuget.org/packages/ZeroInstall.Services.Interfaces/)  
Interfaces/abstractions for types from `ZeroInstall.Services` package

[ZeroInstall.DesktopIntegration](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)  
Methods for integrating applications with desktop environments (creating menu entries, etc.)

[ZeroInstall.Commands](https://www.nuget.org/packages/ZeroInstall.Commands/)  
Command-line interface for Zero Install (both an actual CLI and a library for building other clients)

[ZeroInstall.Publish](https://www.nuget.org/packages/ZeroInstall.Publish/)  
Utilities for creating and modifying feed files

## Building and contributing

See the **[GitHub project](https://github.com/0install/0install-dotnet)** for information on how to build the source yourself and how to contribute.
