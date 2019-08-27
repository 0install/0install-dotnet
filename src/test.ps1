$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

dotnet test --no-build --configuration Release UnitTests\UnitTests.csproj
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

popd
