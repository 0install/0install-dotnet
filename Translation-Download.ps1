Param ([Parameter(Mandatory=$True)][String]$User, [Parameter(Mandatory=$True)][String]$Password)
$ErrorActionPreference = "Stop"

function get ($relativeUri, $filePath) {
    curl.exe -k -L --user "${User}:${Password}" -o $filePath https://www.transifex.com/api/2/project/0install-win/$relativeUri
}

function download($slug, $pathBase) {
    get "resource/$slug/translation/el/?file" "$pathBase.el.resx"
    get "resource/$slug/translation/tr/?file" "$pathBase.tr.resx"
}

download model "$PSScriptRoot\src\Model\Properties\Resources"
download store "$PSScriptRoot\src\Store\Properties\Resources"
download archives "$PSScriptRoot\src\Archives\Properties\Resources"
download services "$PSScriptRoot\src\Services\Properties\Resources"
download desktopintegration "$PSScriptRoot\src\DesktopIntegration\Properties\Resources"
download commands "$PSScriptRoot\src\Commands\Properties\Resources"
download publish "$PSScriptRoot\src\Publish\Properties\Resources"
