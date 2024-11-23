# Zero Install .NET

[![API documentation](https://img.shields.io/badge/api-docs-orange.svg)](https://dotnet.0install.net/)
[![Build status](https://img.shields.io/appveyor/ci/0install/0install-dotnet.svg)](https://ci.appveyor.com/project/0install/0install-dotnet)  
Zero Install is a decentralized cross-platform software installation system. You can learn more at [0install.net](https://0install.net/).

This is the .NET implementation of Zero Install. It can be used as a [command-line tool](#command-line) on various platforms or be embedded into .NET applications as a set of [libraries](#libraries). It provides the basis for [Zero Install for Windows](https://github.com/0install/0install-win).

## Command-line

Zero Install .NET provides the `0install` command-line tool. There are a number of ways you can get it:

- You can get a .NET Framework version of the `0install` command by downloading [Zero Install for Windows](https://get.0install.net/#windows). (recommended)

- If you already have some version of Zero Install (.NET-based or otherwise) on your system you can use it to download and run a .NET 6 version of the `0install` command like this:

      0install run https://apps.0install.net/0install/0install-dotnet.xml

- You can also manually install [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) and then download a [Zero Install .NET Release](https://github.com/0install/0install-dotnet/releases), extract the archive and run:

      dotnet 0install.dll

## NuGet packages

You can use these NuGet packages to integrate Zero Install features into your own application:

[![ZeroInstall.Model](https://img.shields.io/nuget/v/ZeroInstall.Model.svg?label=ZeroInstall.Model)](https://www.nuget.org/packages/ZeroInstall.Model/)  
Data model for the [feed format](https://docs.0install.net/specifications/feed/).

[![ZeroInstall.Client](https://img.shields.io/nuget/v/ZeroInstall.Client.svg?label=ZeroInstall.Client)](https://www.nuget.org/packages/ZeroInstall.Client/)  
Client for invoking Zero Install commands from within other applications.

[![ZeroInstall.Store](https://img.shields.io/nuget/v/ZeroInstall.Store.svg?label=ZeroInstall.Store)](https://www.nuget.org/packages/ZeroInstall.Store/)  
Management of [implementation caches](https://docs.0install.net/details/cache/), digital signatures, etc..

[![ZeroInstall.Archives](https://img.shields.io/nuget/v/ZeroInstall.Archives.svg?label=ZeroInstall.Archives)](https://www.nuget.org/packages/ZeroInstall.Archives/)  
Extracting and building archives (`.zip`, `.tar`, etc.).

[![ZeroInstall.Services](https://img.shields.io/nuget/v/ZeroInstall.Services.svg?label=ZeroInstall.Services)](https://www.nuget.org/packages/ZeroInstall.Services/)  
Core services like solving dependencies, downloading implementations and execution selections.  
Zero Install itself is built upon this API. You can use the API to integrate Zero Install features into your own application.

[![ZeroInstall.DesktopIntegration](https://img.shields.io/nuget/v/ZeroInstall.DesktopIntegration.svg?label=ZeroInstall.DesktopIntegration)](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)  
Integrating applications with [desktop environments](https://docs.0install.net/details/desktop-integration/) (creating menu entries, etc.).

[![ZeroInstall.Commands](https://img.shields.io/nuget/v/ZeroInstall.Commands.svg?label=ZeroInstall.Commands)](https://www.nuget.org/packages/ZeroInstall.Commands/)  
Command-line interface for Zero Install.  
The binary in this package serves both as an actual CLI and a library for building other clients.

[![ZeroInstall.Publish](https://img.shields.io/nuget/v/ZeroInstall.Publish.svg?label=ZeroInstall.Publish)](https://www.nuget.org/packages/ZeroInstall.Publish/)  
Utilities for creating and modifying feed files.  
The [Zero Install Publishing Tools](https://github.com/0install/0publish-win) (including the Feed Editor) are built upon this library. You can use this to automate complex feed creation/update tasks.

For more information read the [Zero Install .NET API documentation](https://dotnet.0install.net/).

## Building

The source code is in [`src/`](src/), config for building the API documentation is in [`doc/`](doc/) and generated artifacts are placed in `artifacts/`.  
The source code does not contain version numbers. Instead the version is determined during CI using [GitVersion](https://gitversion.net/).

To build run `.\build.ps1` or `./build.sh` (.NET SDK is automatically downloaded if missing).

## Contributing

We welcome contributions to this project such as bug reports, recommendations, pull requests and [translations](https://www.transifex.com/eicher/0install-win/). If you have any questions feel free to pitch in on our [friendly mailing list](https://0install.net/support.html#lists).

This repository contains an [EditorConfig](http://editorconfig.org/) file. Please make sure to use an editor that supports it to ensure consistent code style, file encoding, etc.. For full tooling support for all style and naming conventions consider using JetBrains' [ReSharper](https://www.jetbrains.com/resharper/) or [Rider](https://www.jetbrains.com/rider/) products.
