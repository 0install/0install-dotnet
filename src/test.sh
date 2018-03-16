#!/bin/sh
set -e
cd `dirname $0`

dotnet test --no-build UnitTests/UnitTests.csproj
