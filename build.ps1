Param ([String]$Version = "1.0.0-pre", [Switch]$SkipTest)
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

src\build.ps1 $Version
if (-Not $SkipTest) { src\test.ps1 }
doc\build.ps1
.\0install.ps1 run --batch https://apps.0install.net/0install/0template.xml 0install-dotnet.xml.template version=$Version

popd
