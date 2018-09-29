$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

# Ensure 0install is in the PATH
if (!(Get-Command 0install -ErrorAction SilentlyContinue)) {
    $env:PATH = "$(Resolve-Path ..\artifacts\Release\net45);$env:PATH"
}

if (Test-Path ..\artifacts\Documentation) {rm -Recurse -Force ..\artifacts\Documentation}
mkdir ..\artifacts\Documentation | Out-Null

# Download tag files for external references
Invoke-WebRequest http://common.nanobyte.de/nanobyte-common.tag -OutFile nanobyte-common.tag

0install run --batch http://0install.de/feeds/Doxygen.xml

cp CNAME ..\artifacts\Documentation\

popd
