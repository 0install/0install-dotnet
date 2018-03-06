$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

dotnet test --configuration Release --no-build UnitTests\UnitTests.csproj

popd
