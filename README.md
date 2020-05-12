# Zero Install .NET

[![API documentation](https://img.shields.io/badge/api-docs-orange.svg)](https://dotnet.0install.de/)
[![Build status](https://img.shields.io/appveyor/ci/0install/0install-dotnet.svg)](https://ci.appveyor.com/project/0install/0install-dotnet)  
This is the .NET implementation of Zero Install. It can be used as a [command-line tool](#command-line) on various platforms or be embedded into .NET applications as a set of [libraries](#libraries). It provides the basis for [Zero Install for Windows](https://github.com/0install/0install-win).

Zero Install is a decentralized cross-platform software installation system. You can learn more at [0install.net](http://0install.net/).

## Command-line

Zero Install .NET provides the `0install` command-line tool. There are a number of ways you can get it:

- You can get a .NET Framework version of the `0install` command by downloading [Zero Install for Windows](https://0install.de/downloads/). (recommended)

- If you already have some version of Zero Install (.NET-based or otherwise) on your system you can use it to download and run a .NET Core version of the `0install` command like this:

      0install run https://apps.0install.net/0install/0install-dotnet.xml

- You can also manually install [.NET Core](https://www.microsoft.com/net/download) and then download a [Zero Install .NET Release](https://github.com/0install/0install-dotnet/releases), extract the archive and run:

      dotnet 0install.dll

## NuGet packages

You can use these NuGet packages to integrate Zero Install features into your own application:

[![ZeroInstall.Store](https://img.shields.io/nuget/v/ZeroInstall.Store.svg?label=ZeroInstall.Store)](https://www.nuget.org/packages/ZeroInstall.Store/)  
Data model for the [feed format](https://docs.0install.de/specifications/feed/), signature verification, management of [on-disk caches](https://docs.0install.de/details/cache/).  
This provides a common basis for the packages `ZeroInstall.Services` and `ZeroInstall.Publish`. You will usually get this package indirectly as a dependency from there.

[![ZeroInstall.Services](https://img.shields.io/nuget/v/ZeroInstall.Services.svg?label=ZeroInstall.Services)](https://www.nuget.org/packages/ZeroInstall.Services/)  
Core services like [solving dependencies](https://docs.0install.de/developers/solver/), downloading implementations and execution selections.  
Zero Install itself is built upon this API. You can use the API to integrate Zero Install features into your own application, e.g. for a plugin management system.

[![ZeroInstall.Services.Interfaces](https://img.shields.io/nuget/v/ZeroInstall.Services.Interfaces.svg?label=ZeroInstall.Services.Interfaces)](https://www.nuget.org/packages/ZeroInstall.Services.Interfaces/)  
Interfaces/abstractions for Zero Install services (contains no actual implementations).  
You will usually get this package indirectly as a dependency of `ZeroInstall.Services`.

[![ZeroInstall.DesktopIntegration](https://img.shields.io/nuget/v/ZeroInstall.DesktopIntegration.svg?label=ZeroInstall.DesktopIntegration)](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)  
Methods for integrating applications with desktop environments (creating menu entries, etc.).

[![ZeroInstall.Commands](https://img.shields.io/nuget/v/ZeroInstall.Commands.svg?label=ZeroInstall.Commands)](https://www.nuget.org/packages/ZeroInstall.Commands/)  
Command-line interface for Zero Install.  
The binary in this package serves both as an actual CLI and a library for building other clients.

[![ZeroInstall.Publish](https://img.shields.io/nuget/v/ZeroInstall.Publish.svg?label=ZeroInstall.Publish)](https://www.nuget.org/packages/ZeroInstall.Publish/)  
Utilities for creating and modifying feed files.  
The [Zero Install Publishing Tools](https://github.com/0install/0publish-win) (including the Feed Editor) are built upon this library. You can use this to automate complex feed creation/update tasks.

For more information read the [Zero Install .NET API documentation](https://docs.0install.de/developers/dotnet-api/).

## Building

The source code is in [`src/`](src/), a project for API documentation is in [`doc/`](doc/) and generated build artifacts are placed in `artifacts/`.  
There is a template in [`feed/`](feed/) for generating a [Zero Install feed](https://0install.github.io/docs/packaging/) from the artifacts. For official releases this is published at: http://0install.de/feeds/0install-dotnet.xml

You need [Visual Studio 2019](https://www.visualstudio.com/downloads/) to perform a full build of this project.  
You can build for .NET Standard on Linux using just the [.NET Core SDK 3.1+](https://www.microsoft.com/net/download). Additionally installing [Mono 6.4+](https://www.mono-project.com/download/stable/) allows you to also build for .NET Framework. The build scripts will automatically adjust accordingly.

Run `.\build.ps1` on Windows or `./build.sh` on Linux. These scripts take a version number as an input argument. The source code itself contains no version numbers. Instead the version is picked by continuous integration using [GitVersion](http://gitversion.readthedocs.io/).

## Contributing

We welcome contributions to this project such as bug reports, recommendations, pull requests and [translations](https://www.transifex.com/eicher/0install-win/). If you have any questions feel free to pitch in on our [friendly mailing list](http://0install.net/support.html#lists).

This repository contains an [EditorConfig](http://editorconfig.org/) file. Please make sure to use an editor that supports it to ensure consistent code style, file encoding, etc.. For full tooling support for all style and naming conventions consider using JetBrain's [ReSharper](https://www.jetbrains.com/resharper/) or [Rider](https://www.jetbrains.com/rider/) products.
