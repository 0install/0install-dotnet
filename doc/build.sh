#!/bin/bash
set -e
cd `dirname $0`

rm -rf ../build/Documentation
mkdir -p ../build/Documentation

# Download tag files for external references
curl -o nanobyte-common.tag http://nano-byte.de/common/api/nanobyte-common.tag

0install run http://0install.de/feeds/Doxygen.xml
