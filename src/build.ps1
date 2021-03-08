Param ($Version = "1.0.0-pre")
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

# Build
if ($env:CI) { $ci = "/p:ContinuousIntegrationBuild=True" }
Run-DotNet msbuild /v:Quiet /t:Restore /t:Build /p:Configuration=Release /p:Version=$Version $ci

# Package .NET Core distribution
Run-DotNet msbuild /v:Quiet /t:Publish /p:NoBuild=True /p:BuildProjectReferences=False /p:Configuration=Release /p:TargetFramework=net5.0 /p:Version=$Version Commands
tar -czf ..\artifacts\0install-dotnet-$Version.tar.gz -C ..\artifacts\Release\net5.0\publish --exclude *.pdb *

popd
