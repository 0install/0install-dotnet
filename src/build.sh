#!/usr/bin/env bash
set -e
cd `dirname $0`

# Find dotnet
if command -v dotnet > /dev/null 2> /dev/null; then
    dotnet="dotnet"
else
    dotnet="../0install.sh run --version 5.0.. https://apps.0install.net/dotnet/sdk.xml"
fi

# Build
$dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=${1:-1.0.0-pre} ${CI+-p:ContinuousIntegrationBuild=True}

# Package .NET Core distribution
$dotnet msbuild -v:Quiet -t:Publish -p:NoBuild=True -p:BuildProjectReferences=False -p:Configuration=Release -p:TargetFramework=net5.0 -p:Version=${1:-1.0.0-pre} Commands
cd ../artifacts/Release/net5.0/publish
tar -czf ../../../0install-dotnet-${1:-1.0.0-pre}.tar.gz --exclude '*.pdb' *
