Param ($Version = "0.1.0-pre")
$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

src\build.ps1 $Version
src\test.ps1
doc\build.ps1

popd
