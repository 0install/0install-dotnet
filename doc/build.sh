#!/bin/bash
set -e
cd `dirname $0`

rm -rf ../build/Documentation
mkdir -p ../build/Documentation
0install run http://0install.de/feeds/Doxygen.xml
