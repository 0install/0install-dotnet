#!/bin/sh
set -e
cd `dirname $0`

# Find dotnet
if command -v dotnet > /dev/null 2> /dev/null; then
    dotnet="dotnet"
else
    dotnet="../0install.sh run --version 3.1..!3.2 https://apps.0install.net/dotnet/core-sdk.xml"
fi

# Unit tests (without .NET Framework)
$dotnet test --no-build --configuration Release --framework netcoreapp3.1 UnitTests/UnitTests.csproj
