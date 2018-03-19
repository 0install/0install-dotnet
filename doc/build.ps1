$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

# Ensure 0install is in the PATH
if (!(Get-Command 0install -ErrorAction SilentlyContinue)) {
	mkdir -Force "$env:TEMP\zero-install" | Out-Null
	Invoke-WebRequest "https://0install.de/files/zero-install.exe" -OutFile "$env:TEMP\zero-install\0install.exe"
	$env:PATH = "$env:TEMP\zero-install;$env:PATH"
}

if (Test-Path ..\build\Documentation) {rm -Recurse -Force ..\build\Documentation}
mkdir ..\build\Documentation | Out-Null
0install run --batch http://0install.de/feeds/Doxygen.xml

popd
