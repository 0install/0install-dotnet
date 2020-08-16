#!/bin/bash
set -e
cd `dirname $0`

# Inspect version number
version=${1:-1.0.0-pre}
if [[ $version == *"-"* ]]; then
  stability="developer"
else
  stability="stable"
fi

# Build feed and archive
../0install.sh run http://0install.net/tools/0template.xml 0install-dotnet.xml.template version=$version stability=$stability
