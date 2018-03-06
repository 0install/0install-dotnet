#!/bin/sh
set -e
cd `dirname $0`

dotnet test --configuration Release --no-build UnitTests/UnitTests.csproj
