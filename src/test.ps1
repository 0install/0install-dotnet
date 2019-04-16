$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

dotnet test --no-build --configuration Release UnitTests\UnitTests.csproj

popd
