#!/bin/bash
set -e
cd `dirname $0`

# Ensure 0install is in PATH
if ! command -v 0install > /dev/null 2> /dev/null; then
    echo "Downloading 0install..."
    download_dir=$(mktemp --directory)
    curl -sSL https://get.0install.net/0install-$(uname | tr '[:upper:]' '[:lower:]')-$(uname -m)-latest.tar.bz2 | tar xj --strip-components=1 --directory=$download_dir
    PATH=$PATH:$download_dir/files
fi

echo "Downloading references to other documentation..."
curl -sS -o nanobyte-common.tag https://common.nano-byte.net/nanobyte-common.tag

rm -rf ../artifacts/Documentation
mkdir -p ../artifacts/Documentation

echo "Generating API documentation..."
VERSION=${1:-1.0.0-pre}
0install run https://apps.0install.net/devel/doxygen.xml
