#!/bin/sh
set -e
cd `dirname $0`

# Find dotnet
if command -v dotnet > /dev/null 2> /dev/null; then
    dotnet="dotnet"
else
    dotnet="../0install.sh run --version 5.0..!5.1 https://apps.0install.net/dotnet/core-sdk.xml"
fi

# Unit tests (without .NET Framework)
$dotnet test --no-build --configuration Release --framework net5.0 UnitTests/UnitTests.csproj
