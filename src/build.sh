#!/usr/bin/env bash
set -e
cd `dirname $0`

# Find dotnet
if command -v dotnet > /dev/null 2> /dev/null; then
    dotnet="dotnet"
else
    dotnet="../0install.sh run --version 10.0..!10.1 https://apps.0install.net/dotnet/sdk.xml"
fi

# Avoid terminal rendering issues in CI
if [ -n "$CI" ]; then
    export MSBUILDTERMINALLOGGER="off"
fi

echo "Build binaries"
$dotnet msbuild -v:Quiet -restore -t:Build -p:Configuration=Release -p:Version=${1:-1.0.0-pre} ${CI+-p:ContinuousIntegrationBuild=True}

echo "Prepare binaries for publishing"
$dotnet msbuild -v:Quiet -t:Publish -p:NoBuild=True -p:BuildProjectReferences=False -p:Configuration=Release -p:TargetFramework=net10.0 -p:Version=${1:-1.0.0-pre} Commands
find ../artifacts/Release/net10.0/publish -name "{*.xml,*.pdb}" -type f -delete

echo "Build minimal binaries"
$dotnet msbuild -v:Quiet -restore -t:Build -p:Configuration=Minimal -p:Version=${1:-1.0.0-pre}

case "$(uname -s)-$(uname -m)" in
    Linux-x86_64) rid="linux-x64" ;;
    Linux-aarch64 | Linux-arm64) rid="linux-arm64" ;;
    Darwin-x86_64) rid="osx-x64" ;;
    Darwin-arm64) rid="osx-arm64" ;;
    *) rid="" ;;
esac
if [ -n "$rid" ]; then
    echo "Link bootstrapper"
    $dotnet msbuild -v:Quiet -t:Publish -p:NoBuild=True -p:BuildProjectReferences=False -p:Configuration=Minimal -p:TargetFramework=net10.0 -p:RuntimeIdentifier=$rid -p:Version=${1:-1.0.0-pre} Bootstrap.Cli
fi
