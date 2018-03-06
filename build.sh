#!/bin/sh
set -e
cd `dirname $0`

src/build.sh
src/test.sh
doc/build.sh
