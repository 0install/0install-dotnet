Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

echo "Downloading references to other documentation..."
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]'Tls11,Tls12'
Invoke-WebRequest https://common.nano-byte.net/nanobyte-common.tag -OutFile nanobyte-common.tag

if (Test-Path ..\artifacts\Documentation) {rm -Recurse -Force ..\artifacts\Documentation}
mkdir ..\artifacts\Documentation | Out-Null

$env:VERSION = $Version
..\0install.ps1 run --batch https://apps.0install.net/devel/doxygen.xml
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

popd
