#!/bin/sh
set -e
cd `dirname $0`

# Find dotnet
#if command -v dotnet > /dev/null 2> /dev/null; then
#    dotnet="dotnet"
#else
    dotnet="../0install.sh run --version 9.0.200.. https://apps.0install.net/dotnet/sdk.xml"
#fi

echo "Build docs"
rm -rf api/
$dotnet tool restore
$dotnet docfx --logLevel=warning --warningsAsErrors docfx.json
