Zero Install .NET Backend
=========================

The Zero Install .NET Backend implements the core features of Zero Install. It also allow developers to embed Zero Install functionality in their own .NET applications. It is available for .NET Framework 2.0 or newer and .NET Standard 2.0 or newer.

[Zero Install for Windows](https://github.com/0install/0install-win) and the [Zero Install Publishing Tools](https://github.com/0install/0publish-win) are built upon this backend.

[![TeamCity Build status](https://0install.de/teamcity/app/rest/builds/buildType:(id:ZeroInstall_DotNetBackend_Build)/statusIcon)](https://0install.de/teamcity/viewType.html?buildTypeId=ZeroInstall_DotNetBackend_Build&guest=1)

**[API documentation](http://0install.de/api/backend/)**

NuGet packages:
- **[ZeroInstall.Store](https://www.nuget.org/packages/ZeroInstall.Store/)** (data models and management functions for local file storage)
- **[ZeroInstall.Services](https://www.nuget.org/packages/ZeroInstall.Services/)** (services such as dependency resolution and implementation downloading)
- **[ZeroInstall.Services.Interfaces](https://www.nuget.org/packages/ZeroInstall.Services.Interfaces/)** (interfaces and extension methods)
- **[ZeroInstall.DesktopIntegration](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)** (methods for integrating applications with desktop environments, creating menu entries, etc.)
- **[ZeroInstall.Publish](https://www.nuget.org/packages/ZeroInstall.Publish/)** (methods for creating and modifying feed files)

Directory structure
-------------------
- `src` contains source code.
- `lib` contains pre-compiled 3rd party libraries which are not available via NuGet.
- `doc` contains a Doxyfile project for generation the API documentation.
- `build` contains the results of various compilation processes. It is created on first usage.
- `samples` contains code snippets in different languages illustrating how to use the Zero Install Backend.

Building
--------
You need to install [Visual Studio 2017](https://www.visualstudio.com/downloads/) and [Zero Install](http://0install.de/downloads/) to perform a full build of this project.  
You can work on the cross-platform parts of the library using the [.NET Core SDK](https://www.microsoft.com/net/download) or [Mono](https://www.mono-project.com/download/stable/).

Run `.\build.ps1` on Windows or `./build.sh` on Linux to build and run unit tests. These scripts takes a version number as an input argument. The source code itself contains no version numbers. Instead the version is picked by continous integration using [GitVersion](http://gitversion.readthedocs.io/).
