#!/bin/sh
set -e
cd `dirname $0`
VERSION=${1:-1.0.0-pre}

src/build.sh $VERSION
[ "$2" != "--skip-test" ] && src/test.sh
doc/build.sh
./0install.sh run https://apps.0install.net/0install/0template.xml 0install-dotnet.xml.template version=$VERSION
