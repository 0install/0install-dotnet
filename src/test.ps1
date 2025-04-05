$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

function Run-DotNet {
    ..\0install.ps1 run --batch --version 9.0.200..!9.1 https://apps.0install.net/dotnet/sdk.xml @args
    if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}
}

echo "Unit tests"
Run-DotNet test --verbosity quiet --no-build --configuration Release UnitTests\UnitTests.csproj

popd
