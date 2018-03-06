#!/bin/sh
set -e
cd `dirname $0`

msbuild -v:Quiet -t:Clean
msbuild -t:Restore -t:Build -p:Configuration=Release
