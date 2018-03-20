Zero Install .NET Backend
=========================

The Zero Install .NET Backend implements the core features of Zero Install. It also allow developers to embed Zero Install functionality in their own .NET applications. It is available for .NET Framework 2.0 or newer and .NET Standard 2.0 or newer.

[Zero Install for Windows](https://github.com/0install/0install-win) and the [Zero Install Publishing Tools](https://github.com/0install/0publish-win) are built upon this backend.

NuGet packages:  
[![ZeroInstall.Store](https://img.shields.io/nuget/v/ZeroInstall.Store.svg?label=ZeroInstall.Store)](https://www.nuget.org/packages/ZeroInstall.Store/)
[![ZeroInstall.Services](https://img.shields.io/nuget/v/ZeroInstall.Services.svg?label=ZeroInstall.Services)](https://www.nuget.org/packages/ZeroInstall.Services/)
[![ZeroInstall.DesktopIntegration](https://img.shields.io/nuget/v/ZeroInstall.DesktopIntegration.svg?label=ZeroInstall.DesktopIntegration)](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)
[![ZeroInstall.](https://img.shields.io/nuget/v/ZeroInstall.Publish.svg?label=ZeroInstall.Publish)](https://www.nuget.org/packages/ZeroInstall.Publish/)

[![API documentation](https://img.shields.io/badge/api-docs-orange.svg)](http://0install.de/api/backend/)
[![Build status](https://img.shields.io/appveyor/ci/0install/0install-dotnet.svg)](https://ci.appveyor.com/project/0install/0install-dotnet)

Directory structure
-------------------
- `src` contains source code.
- `lib` contains pre-compiled 3rd party libraries which are not available via NuGet.
- `doc` contains a Doxyfile project for generation the API documentation.
- `build` contains the results of various compilation processes. It is created on first usage.
- `samples` contains code snippets in different languages illustrating how to use the Zero Install Backend.

Building
--------
You need to install [Visual Studio 2017](https://www.visualstudio.com/downloads/) to perform a full build of this project.  
The cross-platform parts also build using the [.NET Core SDK 2.1+](https://www.microsoft.com/net/download) or [Mono 5.10+](https://www.mono-project.com/download/stable/).

Run `.\build.ps1` on Windows or `./build.sh` on Linux to build and run unit tests. These scripts takes a version number as an input argument. The source code itself contains no version numbers. Instead the version is picked by continous integration using [GitVersion](http://gitversion.readthedocs.io/).
