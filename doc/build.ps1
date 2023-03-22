$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

..\0install.ps1 run --batch --version=..!2.60 https://apps.0install.net/dotnet/docfx.xml --loglevel=warning --warningsAsErrors docfx.json
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

popd
