$ErrorActionPreference = "Stop"

function Download-ZeroInstall {
    $dir = "$env:LOCALAPPDATA\0install.net\bootstrapper"
    $file = "$dir\0install.exe"
    if (!(Test-Path $file)) {
        mkdir -Force $dir | Out-Null
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]'Tls11,Tls12'
        Invoke-WebRequest "https://get.0install.net/0install.exe" -OutFile $file
    }
    return $file
}

function Run-ZeroInstall {
    if (Get-Command 0install -ErrorAction SilentlyContinue) {
        0install @args
    } else {
        . $(Download-ZeroInstall) @args
    }
}

if ($args.Count -eq 0) {
    echo "This script runs 0install from your PATH or downloads it on-demand."
    echo ""
    echo "To run 0install commands without adding 0install to your PATH:"
    echo ".\0install.ps1 COMMAND [OPTIONS]"
    echo ""
    echo "To deploy 0install to your user profile:"
    echo ".\0install.ps1 self deploy"
    echo ""
    echo "To deploy 0install to your machine:"
    echo ".\0install.ps1 self deploy --machine"
} else {
    Run-ZeroInstall @args
}
