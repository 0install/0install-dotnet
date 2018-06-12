Param ($Version = "1.0-dev")
$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

if (Test-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe") {
  $vsDir = . "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath -format value
  $msBuild = "$vsDir\MSBuild\15.0\Bin\amd64\MSBuild.exe"
} else {
  Write-Host -ForegroundColor yellow "WARNING: You need Visual Studio to perform a full Release build"
  $msBuild = "dotnet msbuild"
}
. $msBuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=$Version

# Create snapshot of XML Schemas
if (!(Test-Path ..\build\Schemas)) { mkdir ..\build\Schemas | Out-Null }
cp *\*.xsd,*\*\*.xsd ..\build\Schemas

popd
