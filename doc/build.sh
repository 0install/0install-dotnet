#!/bin/bash
set -e
cd `dirname $0`

echo "Downloading references to other documentation..."
curl -sS -o nanobyte-common.tag https://common.nano-byte.net/nanobyte-common.tag

rm -rf ../artifacts/Documentation
mkdir -p ../artifacts/Documentation

VERSION=${1:-1.0-dev} ../0install.sh run https://apps.0install.net/devel/doxygen.xml
