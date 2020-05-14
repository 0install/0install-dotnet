#!/bin/bash
set -e
cd `dirname $0`

dotnet msbuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=${1:-1.0.0-pre}

# Create snapshot of XML Schemas
mkdir -p ../artifacts/Schemas
cp */*.xsd */*/*.xsd ../artifacts/Schemas
