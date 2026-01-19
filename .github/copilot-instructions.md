# Zero Install .NET - Copilot Instructions

## Repository Overview

This is the .NET implementation of Zero Install, a decentralized cross-platform software installation system. The repository contains ~54,000 lines of C# code implementing the core Zero Install functionality as a set of NuGet libraries and a command-line tool.

**Key Technologies:**
- **Language:** C# with preview language features (`LangVersion: preview`)
- **Frameworks:** Multi-targeting .NET Framework 4.7.2, .NET 8.0, and .NET 9.0
- **Build System:** MSBuild with custom scripts
- **Testing:** xUnit with FluentAssertions and Moq
- **CI/CD:** AppVeyor for builds and releases
- **Versioning:** GitVersion (ContinuousDeployment mode)
- **License:** LGPL-3.0-or-later

## Build Instructions

### Prerequisites
- .NET SDK 9.0+ or .NET SDK 10.0+ (installed at `/usr/bin/dotnet`)
- The build scripts (`0install.sh`/`0install.ps1`) attempt to download .NET SDK via Zero Install, but this may fail in restricted environments

### Building the Code

**ALWAYS use the local dotnet installation directly if the 0install scripts fail to download dependencies.**

#### Successful Build Command (Verified):
```bash
cd src
dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=1.0.0-pre
```

**Build time:** ~30-60 seconds for full build

#### Build Configurations:
- **Release:** Full build with NuGet packages, XML docs, and strong-name signing
- **Minimal:** Reduced build without DesktopIntegration, Commands, Publish, and Client projects
- **Debug:** Not commonly used

#### Build Outputs:
- Binaries: `artifacts/Release/net{framework}/`
- NuGet packages: `artifacts/Release/*.nupkg`
- Published binaries: `artifacts/Release/net8.0/publish/`

### Running Tests

**Verified test command:**
```bash
cd src
dotnet test --verbosity quiet --no-build --configuration Release --framework net9.0 UnitTests/UnitTests.csproj
```

**Test time:** ~7-10 seconds  
**Note:** Some tests may fail (4 known failures as of testing). Focus on tests related to your changes.

**IMPORTANT:** Tests require the code to be built first with `--no-build` flag. If running tests without prior build, remove `--no-build`.

### Common Build Issues and Workarounds

1. **0install.sh download failures:** The helper scripts try to download Zero Install from `get.0install.net` which may be blocked. Use local `dotnet` directly instead.

2. **Version numbers:** The codebase uses GitVersion. For local builds, pass `1.0.0-pre` or similar as the version parameter.

3. **Terminal logger issues in CI:** The build scripts set `MSBUILDTERMINALLOGGER=off` in CI environments to avoid rendering issues.

## Project Structure

### Source Organization (`src/`)

The solution is organized into distinct NuGet packages:

- **Model/** - Data model for Zero Install feed format (`ZeroInstall.Model`)
- **Store/** - Implementation cache management, digital signatures, icons, feeds (`ZeroInstall.Store`)
- **Archives/** - Archive extraction (.zip, .tar, etc.) (`ZeroInstall.Archives`)
- **Services/** - Core services: dependency solving, downloading, feed management (`ZeroInstall.Services`)
- **DesktopIntegration/** - Desktop integration (menu entries, file associations) (`ZeroInstall.DesktopIntegration`)
- **Commands/** - CLI implementation (`ZeroInstall.Commands`) - Contains `Program.cs` entry point
- **Publish/** - Feed creation and editing utilities (`ZeroInstall.Publish`)
- **Client/** - Client API for invoking Zero Install from other apps (`ZeroInstall.Client`)
- **UnitTests/** - xUnit test suite for all projects

### Key Configuration Files

- **`src/Directory.Build.props`** - Global MSBuild properties, target frameworks, NuGet metadata
- **`src/ZeroInstall.slnx`** - Solution file (XML-based .slnx format)
- **`.editorconfig`** - Code style conventions (see below)
- **`GitVersion.yml`** - Version number generation configuration
- **`appveyor.yml`** - CI/CD pipeline (Windows + Ubuntu builds)

### Root Directory Files
- `build.sh`/`build.ps1` - Top-level build orchestration
- `0install.sh`/`0install.ps1` - Zero Install bootstrapping helpers
- `README.md` - Main documentation
- `COPYING.txt` - LGPL license text
- `icon.png`/`icon.ico`/`logo.svg` - Project icons
- `doc/` - API documentation source (DocFX)
- `samples/` - Example code in multiple languages

## Continuous Integration

### AppVeyor Pipeline

The CI runs on both Windows (Visual Studio 2022) and Ubuntu (Ubuntu2004):

**Windows build steps:**
1. `gitversion /verbosity quiet /output buildserver` - Generate version
2. `powershell .\build.ps1 %GitVersion_NuGetVersion% -SkipTest` - Build without tests
3. `powershell src\test.ps1` - Run tests separately

**Ubuntu build steps:**
1. `gitversion /verbosity quiet /output buildserver` - Generate version
2. `src/build.sh $GitVersion_NuGetVersion` - Build code
3. `src/test.sh` - Run tests

**Artifacts produced:**
- NuGet packages (`artifacts/Release/*.nupkg`)
- Documentation (`artifacts/Documentation` → docs.zip)
- Distributable archive (`0install-dotnet-*.tar.gz`)
- Feed file (`0install-dotnet-*.xml`)

**Deployments (on tagged releases):**
- GitHub Releases
- NuGet.org
- SignPath (code signing)
- Documentation to dotnet.0install.net

### GitHub Workflows

- **release.yml** - Post-release actions (publish docs to gh-pages, update feed)
- **translate.yml** / **translate-upload.yml** - Translation management via Transifex

## Code Style Conventions

The project uses `.editorconfig` with strict conventions. **Key rules:**

### File Encoding & Line Endings
- UTF-8 encoding for most files
- LF line endings by default
- CRLF for `.cs`, `.ps1`, `.psd1`, `.bat`, `.cmd`, `.csproj`, `.slnx`, XML files
- Latin-1 encoding for `.bat`, `.cmd`

### Indentation
- 4 spaces for C# and most files
- 2 spaces for JSON, XML, YAML

### C# Style
- **Braces:** Always on new line (Allman style), required for multiline `if/for/foreach/while/using/lock`
- **Var usage:** Explicit types for built-in types, `var` when type is apparent
- **Expression bodies:** Preferred for accessors, methods, properties; not for constructors
- **Pattern matching:** Preferred over `is`/`as` with null checks
- **Null propagation:** Use `?.` and `??` operators
- **Warnings as errors:** `TreatWarningsAsErrors=True` in all projects
- **No XML comments warnings:** Suppressed (NoWarn: 1591)

### Naming & Spacing
- No space after cast: `(int)value`
- Space after keywords: `if (condition)`
- Space around binary operators
- No qualification for members (`this.` not used)

## Dependencies and Packages

**External dependencies:**
- **NanoByte.Common** - Common utilities library
- **Generator.Equals** - Source generator for equality
- **Microsoft.SourceLink.GitHub** - Source link support
- **SharpZipLib**, **SharpCompress**, **ZstdSharp.Port** - Archive handling
- **System.Drawing.Common**, **System.IO.Pipelines** - System libraries
- **ELFSharp** - ELF binary parsing

**Test dependencies:**
- **xUnit** (2.9+), **xunit.runner.visualstudio**, **Xunit.SkippableFact**
- **FluentAssertions** (7.x, not 8.x)
- **Moq** (4.x)
- **Microsoft.Extensions.Configuration** and **DependencyInjection** (version-specific)

**Renovate bot** automatically updates dependencies, with auto-merge for testing, tooling, and Microsoft Extensions packages.

## Making Changes

### Before Committing

1. **Build the code:**
   ```bash
   cd src
   dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=1.0.0-pre
   ```

2. **Run tests:**
   ```bash
   cd src
   dotnet test --verbosity quiet --no-build --configuration Release --framework net9.0 UnitTests/UnitTests.csproj
   ```

3. **Verify your changes compile for all target frameworks** (.NET Framework 4.7.2, .NET 8.0, .NET 9.0)

### Common Patterns

- **Internals visible to tests:** All projects expose internals to `ZeroInstall.UnitTests` via `InternalsVisibleTo`
- **Global usings:** Many common namespaces (System.ComponentModel, System.Net, NanoByte.Common, etc.) are globally imported
- **Multi-targeting:** Most libraries target `net472;net8.0;net9.0`
- **Nullable annotations:** Enabled but not strict (`Nullable: annotations`)

### Documentation Generation

Documentation is built using DocFX:
```bash
cd doc
dotnet tool restore
dotnet docfx --logLevel=warning --warningsAsErrors docfx.json
```

Output: `doc/docs/` directory

## Important Notes

- **Do not modify version numbers in code** - Versions are set by GitVersion during CI builds
- **Respect multi-targeting** - Changes must work on .NET Framework 4.7.2, .NET 8.0, and .NET 9.0
- **Follow .editorconfig** - Use a compatible editor (ReSharper/Rider recommended for full support)
- **Test on affected target frameworks** - Use `--framework` flag to target specific runtimes
- **Check assembly signing** - Release builds use strong-name signing
- **Update XML docs if adding public APIs** - Though warnings are suppressed, documentation should be maintained

## Trust These Instructions

These instructions have been validated by running the actual commands. If you encounter issues:
1. Check that you're using the correct working directory (`src/` for most commands)
2. Verify .NET SDK is available (`dotnet --version`)
3. Try using local `dotnet` directly if 0install scripts fail
4. Ensure previous build artifacts are present when using `--no-build`

Only search for additional information if these instructions are incomplete or proven incorrect for your specific task.
