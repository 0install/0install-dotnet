Zero Install .NET 
==================

Zero Install .NET implements the core features of Zero Install as a command-line interface. It also allow developers to embed Zero Install functionality in their own .NET applications. It is available for .NET Framework 2.0 or newer and .NET Standard 2.0 or newer.

[Zero Install for Windows](https://github.com/0install/0install-win) and the [Zero Install Publishing Tools](https://github.com/0install/0publish-win) are built upon Zero Install .NET.

NuGet packages (for .NET Framework 2.0+ and .NET Standard 2.0+):  
[![ZeroInstall.Store](https://img.shields.io/nuget/v/ZeroInstall.Store.svg?label=ZeroInstall.Store)](https://www.nuget.org/packages/ZeroInstall.Store/)
[![ZeroInstall.Services](https://img.shields.io/nuget/v/ZeroInstall.Services.svg?label=ZeroInstall.Services)](https://www.nuget.org/packages/ZeroInstall.Services/)
[![ZeroInstall.Services.Interfaces](https://img.shields.io/nuget/v/ZeroInstall.Services.Interfaces.svg?label=ZeroInstall.Services.Interfaces)](https://www.nuget.org/packages/ZeroInstall.Services.Interfaces/)
[![ZeroInstall.DesktopIntegration](https://img.shields.io/nuget/v/ZeroInstall.DesktopIntegration.svg?label=ZeroInstall.DesktopIntegration)](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)
[![ZeroInstall.Commands](https://img.shields.io/nuget/v/ZeroInstall.Commands.svg?label=ZeroInstall.Commands)](https://www.nuget.org/packages/ZeroInstall.Commands/)
[![ZeroInstall.Publish](https://img.shields.io/nuget/v/ZeroInstall.Publish.svg?label=ZeroInstall.Publish)](https://www.nuget.org/packages/ZeroInstall.Publish/)

[![API documentation](https://img.shields.io/badge/api-docs-orange.svg)](http://0install.de/api/)
[![Build status](https://img.shields.io/appveyor/ci/0install/0install-dotnet.svg)](https://ci.appveyor.com/project/0install/0install-dotnet)

Directory structure
-------------------
- `src` contains source code.
- `lib` contains pre-compiled 3rd party libraries which are not available via NuGet.
- `doc` contains a Doxygen project for generation the API documentation.
- `artifacts` contains the results of various compilation processes. It is created on first usage.
- `samples` contains code snippets in different languages illustrating how to use the Zero Install .NET API.

Building
--------
You need to install [Visual Studio 2017](https://www.visualstudio.com/downloads/) to perform a full build of this project.  
You can build the cross-platform components on Linux using only the [.NET Core SDK 2.1+](https://www.microsoft.com/net/download). Additionally installing [Mono 5.10+](https://www.mono-project.com/download/stable/) allows more components to be built.

Run `.\build.ps1` on Windows or `./build.sh` on Linux to build and run unit tests. These scripts takes a version number as an input argument. The source code itself contains no version numbers. Instead the version is picked by continuous integration using [GitVersion](http://gitversion.readthedocs.io/).
