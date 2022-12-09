#!/bin/sh
set -e
cd `dirname $0`

dotnet="../0install.sh run --version 7.0.. https://apps.0install.net/dotnet/sdk.xml"

# Unit tests (without .NET Framework)
$dotnet test --no-build --configuration Release --framework net7.0 UnitTests/UnitTests.csproj
