#!/bin/sh
set -e
cd `dirname $0`

# Find dotnet
#if command -v dotnet > /dev/null 2> /dev/null; then
#    dotnet="dotnet"
#else
    dotnet="../0install.sh run --version 8.0.. https://apps.0install.net/dotnet/sdk.xml"
#fi

# Build docs
$dotnet tool restore
$dotnet docfx --logLevel=warning --warningsAsErrors docfx.json
