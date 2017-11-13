Param ([Parameter(Mandatory=$True)][String]$User, [Parameter(Mandatory=$True)][String]$Password)
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

function put ($relativeUri, $filePath) {
    0install run http://repo.roscidus.com/utils/curl -k -L --user "${User}:${Password}" -i -X PUT -F "file=@$filePath" "https://www.transifex.com/api/2/project/0install-win/$relativeUri"
}

function upload($slug, $pathBase) {
    put "resource/$slug/content/" "$pathBase.resx"
    put "resource/$slug/translation/de/" "$pathBase.de.resx"
}

upload store "$ScriptDir\src\Store\Properties\Resources"
upload services "$ScriptDir\src\Services\Properties\Resources"
upload desktopintegration "$ScriptDir\src\DesktopIntegration\Properties\Resources"
upload publish "$ScriptDir\src\Publish\Properties\Resources"
