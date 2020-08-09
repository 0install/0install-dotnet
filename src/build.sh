#!/bin/bash
set -e
cd `dirname $0`

echo "WARNING: You need Visual Studio 2019 v16.5+ to perform a full build of this project" >&2

# Find dotnet
if command -v dotnet > /dev/null 2> /dev/null; then
    dotnet="dotnet"
else
    dotnet="../_0install.sh run --version 3.1..!3.2 https://apps.0install.net/dotnet/core-sdk.xml"
fi

# Run build
$dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=${1:-1.0.0-pre}

# Create snapshot of XML Schemas
mkdir -p ../artifacts/Schemas
cp */*.xsd */*/*.xsd ../artifacts/Schemas
