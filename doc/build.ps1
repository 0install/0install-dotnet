$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

..\0install.ps1 run --batch https://apps.0install.net/dotnet/docfx.xml --logLevel=warning --warningsAsErrors docfx.json
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

popd
