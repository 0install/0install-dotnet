#!/usr/bin/env bash
set -e
shopt -s nullglob

cd $(dirname $0)/content
for f in *.xml; do 0install import "$f"; done
for f in *.tgz; do 0install store add "$(basename "$f" .tgz)" "$f"; done
