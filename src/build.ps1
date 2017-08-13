$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

dotnet restore
. "$(. "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath -format value)\Common7\IDE\devenv.com" ZeroInstall.Backend.sln /Build Release
dotnet test --configuration Release --no-build UnitTests\UnitTests.csproj

popd
