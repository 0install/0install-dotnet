Param ([Parameter(Mandatory=$True)][String]$User, [Parameter(Mandatory=$True)][String]$Password)
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

function get ($relativeUri, $filePath) {
    0install run http://repo.roscidus.com/utils/curl -k -L --user "${User}:${Password}" -o $filePath https://www.transifex.com/api/2/project/0install-win/$relativeUri
}

function download($slug, $pathBase) {
    get "resource/$slug/translation/el/?file" "$pathBase.el.resx"
    get "resource/$slug/translation/tr/?file" "$pathBase.tr.resx"
}

download store "$ScriptDir\src\Store\Properties\Resources"
download services "$ScriptDir\src\Services\Properties\Resources"
download desktopintegration "$ScriptDir\src\DesktopIntegration\Properties\Resources"
download commands "$ScriptDir\src\Commands\Properties\Resources"
download publish "$ScriptDir\src\Publish\Properties\Resources"
