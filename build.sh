#!/bin/sh
set -e
cd `dirname $0`

src/build.sh ${1:-1.0.0-pre}
src/test.sh
doc/build.sh ${1:-1.0.0-pre}
feed/build.sh ${1:-1.0.0-pre}
