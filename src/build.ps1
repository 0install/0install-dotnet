Param ([String]$Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

function Run-DotNet {
    ..\0install.ps1 run --batch --version 7.0.. https://apps.0install.net/dotnet/sdk.xml @args
    if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}
}

# Build
if ($env:CI) { $ci = "/p:ContinuousIntegrationBuild=True" }
Run-DotNet msbuild /v:Quiet /t:Restore /t:Build /p:Configuration=Release /p:Version=$Version $ci

# Prepare for publishing
Run-DotNet msbuild /v:Quiet /t:Publish /p:NoBuild=True /p:BuildProjectReferences=False /p:Configuration=Release /p:TargetFramework=net6.0 /p:Version=$Version Commands
Remove-Item ..\artifacts\Release\net6.0\publish\* -Include *.xml,*.pdb

popd
