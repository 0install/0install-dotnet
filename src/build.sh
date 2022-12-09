#!/usr/bin/env bash
set -e
cd `dirname $0`

# Find dotnet
dotnet="../0install.sh run --version 7.0.. https://apps.0install.net/dotnet/sdk.xml"

# Build
$dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=${1:-1.0.0-pre} ${CI+-p:ContinuousIntegrationBuild=True}

# Package .NET Core distribution
$dotnet msbuild -v:Quiet -t:Publish -p:NoBuild=True -p:BuildProjectReferences=False -p:Configuration=Release -p:TargetFramework=net6.0 -p:Version=${1:-1.0.0-pre} Commands
cd ../artifacts/Release/net6.0/publish
tar -czf ../../../0install-dotnet-${1:-1.0.0-pre}.tar.gz --exclude '*.xml' --exclude '*.pdb' *
