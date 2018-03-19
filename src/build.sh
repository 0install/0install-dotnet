#!/bin/bash
set -e
cd `dirname $0`

echo "WARNING: You need Visual Studio on Windows to perform a full Release build"
if hash msbuild 2>/dev/null; then
    msbuild -v:Quiet -t:Restore -t:Build -p:Version=${1:-1.0-dev}
else
    dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=DebugLinuxCore -p:Version=${1:-1.0-dev}
fi
