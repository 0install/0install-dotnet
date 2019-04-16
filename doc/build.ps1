$ErrorActionPreference = "Stop"
pushd $PSScriptRoot

# Ensure 0install is in PATH
if (!(Get-Command 0install -ErrorAction SilentlyContinue)) {
    $env:PATH = "$(Resolve-Path ..\artifacts\Release\net45);$env:PATH"
}

if (Test-Path ..\artifacts\Documentation) {rm -Recurse -Force ..\artifacts\Documentation}
mkdir ..\artifacts\Documentation | Out-Null

# Download tag files for external references
Invoke-WebRequest https://common.nano-byte.net/nanobyte-common.tag -OutFile nanobyte-common.tag

0install run --batch http://repo.roscidus.com/devel/doxygen

cp CNAME ..\artifacts\Documentation\

popd
