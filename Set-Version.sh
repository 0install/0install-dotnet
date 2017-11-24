#!/bin/bash
set -e
cd `dirname $0`

printf $1 > VERSION
sed -b -i "s/PROJECT_NUMBER = \".*\"/PROJECT_NUMBER = \"$1\"/" doc/Doxyfile
sed -b -i "s/<Version>.*<\/Version>/<Version>$1<\/Version>/" src/Store/Store.csproj
sed -b -i "s/<Version>.*<\/Version>/<Version>$1<\/Version>/" src/Services.Interfaces/Services.Interfaces.csproj
sed -b -i "s/<Version>.*<\/Version>/<Version>$1<\/Version>/" src/Services/Services.csproj
sed -b -i "s/<Version>.*<\/Version>/<Version>$1<\/Version>/" src/DesktopIntegration/DesktopIntegration.csproj
sed -b -i "s/<Version>.*<\/Version>/<Version>$1<\/Version>/" src/Publish/Publish.csproj
