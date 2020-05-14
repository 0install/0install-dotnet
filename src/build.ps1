Param ($Version = "1.0.0-pre")
$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

if (Test-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe") {
  $vsDir = . "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -products * -property installationPath -format value -version 16.5
  $msBuild = if (Test-Path "$vsDir\MSBuild\Current") {"$vsDir\MSBuild\Current\Bin\amd64\MSBuild.exe"} else {"$vsDir\MSBuild\15.0\Bin\amd64\MSBuild.exe"}
} else {
  throw "Needs Visual Studio 2019 v16.5 or newer"
}

. $msBuild -v:Quiet -t:Restore -t:Build -p:Configuration=Release -p:Version=$Version
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

. $msBuild -v:Quiet -t:Publish -p:NoBuild=True -p:BuildProjectReferences=False -p:Configuration=Release -p:TargetFramework=netcoreapp3.1 -p:Version=$Version Commands
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

# Create snapshot of XML Schemas
if (!(Test-Path ..\artifacts\Schemas)) { mkdir ..\artifacts\Schemas | Out-Null }
cp *\*.xsd,*\*\*.xsd ..\artifacts\Schemas

popd
