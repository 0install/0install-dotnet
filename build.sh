#!/bin/sh
set -e
cd `dirname $0`

src/build.ps1
doc/build.ps1
