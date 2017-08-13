#!/bin/sh
set -e
cd `dirname $0`

msbuild /t:Restore /t:Build /p:Configuration=Release
#dotnet test --configuration Release --no-build Common.UnitTests/Common.UnitTests.csproj
#dotnet test --configuration Release --no-build Common.SlimDX.UnitTests/Common.SlimDX.UnitTests.csproj
