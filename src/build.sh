#!/usr/bin/env bash
set -e
cd `dirname $0`

# Find dotnet
if command -v dotnet > /dev/null 2> /dev/null; then
    dotnet="dotnet"
else
    dotnet="../0install.sh run --version 7.0.. https://apps.0install.net/dotnet/sdk.xml"
fi

# Build
$dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=${1:-1.0.0-pre} ${CI+-p:ContinuousIntegrationBuild=True}

# Prepare for publishing
$dotnet msbuild -v:Quiet -t:Publish -p:NoBuild=True -p:BuildProjectReferences=False -p:Configuration=Release -p:TargetFramework=net6.0 -p:Version=${1:-1.0.0-pre} Commands
find ../artifacts/Release/net6.0/publish -name "{*.xml,*.pdb}" -type f -delete
