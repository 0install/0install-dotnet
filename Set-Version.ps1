Param ([Parameter(Mandatory=$True)] [string]$NewVersion)
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

function SearchAndReplace($FilePath, $PatternLeft, $PatternRight)
{
  (Get-Content "$ScriptDir\$FilePath" -Encoding UTF8) `
    -replace "$PatternLeft.*$PatternRight", ($PatternLeft + $NewVersion + $PatternRight) |
    Set-Content "$ScriptDir\$FilePath" -Encoding UTF8
}

[System.IO.File]::WriteAllText("$ScriptDir\VERSION", $NewVersion)
SearchAndReplace doc\Doxyfile -PatternLeft 'PROJECT_NUMBER = "' -PatternRight '"'
SearchAndReplace src\DesktopIntegration\DesktopIntegration.csproj -PatternLeft '<Version>' -PatternRight '</Version>'
SearchAndReplace src\Services\Services.csproj -PatternLeft '<Version>' -PatternRight '</Version>'
SearchAndReplace src\Services.Interfaces\Services.Interfaces.csproj -PatternLeft '<Version>' -PatternRight '</Version>'
SearchAndReplace src\Store\Store.csproj -PatternLeft '<Version>' -PatternRight '</Version>'
