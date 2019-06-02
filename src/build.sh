#!/bin/bash
set -e
cd `dirname $0`

echo "WARNING: You need Visual Studio on Windows to perform a full Release build"

# Prefer Mono's msbuild over .NET Core's
if hash msbuild 2>/dev/null; then
    msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Debug -p:Version=${1:-1.0.0-pre}
else
    dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=DebugLinuxCore -p:Version=${1:-1.0.0-pre}
fi

# Create snapshot of XML Schemas
mkdir -p ../artifacts/Schemas
cp */*.xsd */*/*.xsd ../artifacts/Schemas
