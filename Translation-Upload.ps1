Param ([Parameter(Mandatory=$True)][String]$User, [Parameter(Mandatory=$True)][String]$Password)
$ErrorActionPreference = "Stop"

function put ($relativeUri, $filePath) {
    curl.exe -k -L --user "${User}:${Password}" -i -X PUT -F "file=@$filePath" "https://www.transifex.com/api/2/project/0install-win/$relativeUri"
}

function upload($slug, $pathBase) {
    put "resource/$slug/content/" "$pathBase.resx"
    put "resource/$slug/translation/de/" "$pathBase.de.resx"
}

upload store "$PSScriptRoot\src\Store\Properties\Resources"
upload services "$PSScriptRoot\src\Services\Properties\Resources"
upload desktopintegration "$PSScriptRoot\src\DesktopIntegration\Properties\Resources"
upload commands "$PSScriptRoot\src\Commands\Properties\Resources"
upload publish "$PSScriptRoot\src\Publish\Properties\Resources"
