$ErrorActionPreference = "Stop"
pushd $PSScriptRoot
..\..\0install.ps1 run --batch https://apps.0install.net/utils/graphviz.xml "nuget-dependencies.dot" -Tsvg -o "nuget-dependencies.svg"
popd
