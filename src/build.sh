#!/usr/bin/env bash
set -e
cd `dirname $0`

# Find dotnet
#if command -v dotnet > /dev/null 2> /dev/null; then
#    dotnet="dotnet"
#else
    dotnet="../0install.sh run --version 8.0.. https://apps.0install.net/dotnet/sdk.xml"
#fi

echo "Build binaries"
$dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=${1:-1.0.0-pre} ${CI+-p:ContinuousIntegrationBuild=True -terminalLogger:off}

echo "Prepare binaries for publishing"
$dotnet msbuild -v:Quiet -t:Publish -p:NoBuild=True -p:BuildProjectReferences=False -p:Configuration=Release -p:TargetFramework=net8.0 -p:Version=${1:-1.0.0-pre} Commands
find ../artifacts/Release/net8.0/publish -name "{*.xml,*.pdb}" -type f -delete
