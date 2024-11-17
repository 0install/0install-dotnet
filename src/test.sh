#!/bin/sh
set -e
cd `dirname $0`

# Find dotnet
#if command -v dotnet > /dev/null 2> /dev/null; then
#    dotnet="dotnet"
#else
    dotnet="../0install.sh run --version 8.0..!8.1 https://apps.0install.net/dotnet/sdk.xml"
#fi

echo "Unit tests"
$dotnet test --verbosity quiet --no-build --configuration Release --framework net8.0 UnitTests/UnitTests.csproj
