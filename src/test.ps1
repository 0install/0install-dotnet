$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

function Run-DotNet {
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        dotnet @args
    } else {
        ..\0install.ps1 run --batch --version 5.0..!5.1 https://apps.0install.net/dotnet/core-sdk.xml @args
    }
    if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}
}

Run-DotNet test --no-build --configuration Release UnitTests\UnitTests.csproj

popd
