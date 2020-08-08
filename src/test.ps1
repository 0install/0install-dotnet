$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

function Run-DotNet {
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        dotnet @args
    } else {
        ..\_0install.ps1 run --batch --version 3.1..!3.2 https://apps.0install.net/dotnet/core-sdk.xml @args
    }
    if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}
}

Run-DotNet test --no-build --configuration Release UnitTests\UnitTests.csproj

popd
