Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

src\build.ps1 $Version
src\test.ps1
doc\build.ps1
feed\build.ps1 $Version

popd
