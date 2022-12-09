#!/bin/sh
set -e
cd `dirname $0`

# Find dotnet
dotnet="../0install.sh run --version 6.0..!6.1 https://apps.0install.net/dotnet/sdk.xml"

# Unit tests (without .NET Framework)
$dotnet test --no-build --configuration Release --framework net6.0 UnitTests/UnitTests.csproj
