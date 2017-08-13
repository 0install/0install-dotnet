Zero Install .NET Backend
=========================

The Zero Install .NET Backend implements the core features of Zero Install. It also allow developers to embed Zero Install functionality in their own .NET applications. [Zero Install for Windows](https://github.com/0install/0install-win) is built upon this backend.

NuGet packages:
- **[ZeroInstall.Store](https://www.nuget.org/packages/ZeroInstall.Store/)** (data models and management functions for local file storage)
- **[ZeroInstall.Services](https://www.nuget.org/packages/ZeroInstall.Services/)** (services such as dependency resolution and implementation downloading)
- **[ZeroInstall.Services.Interfaces](https://www.nuget.org/packages/ZeroInstall.Services.Interfaces/)** (interfaces and extension methods)
- **[ZeroInstall.DesktopIntegration](https://www.nuget.org/packages/ZeroInstall.DesktopIntegration/)** (methods for integrating applications with desktop environments, creating menu entries, etc.)

**[API documentation](http://0install.de/api/backend/)**

Directory structure
-------------------
- `src` contains source code.
- `lib` contains pre-compiled 3rd party libraries which are not available via NuGet.
- `doc` contains a Doxyfile project for generation the API documentation.
- `build` contains the results of various compilation processes. It is created on first usage.
- `samples` contains code snippets in different languages illustrating how to use the Zero Install Backend.

Building
--------
- You need to install [Visual Studio 2017 Update 3](https://www.visualstudio.com/downloads/), [.NET Core 2.0 SDK](https://dot.net/core) and [Zero Install](http://0install.de/downloads/) to build this project.
- The file `VERSION` contains the current version number of the project.
- Run `.\Set-Version.ps1 "X.Y.Z"` in PowerShall to change the version number. This ensures that the version also gets set in other locations (e.g. `.csproj` files).
- Run `.\build.ps1` in PowerShell to build everything.
