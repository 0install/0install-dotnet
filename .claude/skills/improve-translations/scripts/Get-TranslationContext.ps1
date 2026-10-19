<#
.SYNOPSIS
Dumps localized strings together with their English source and the code that uses them, for translation review.

.PARAMETER Languages
Language codes as used in the file names (e.g. fr, pt-BR). Defaults to all.

.PARAMETER Projects
Project names under src/. Defaults to all.

.PARAMETER Status
Only include translations that are automatically translated (Auto), manually overriden (Manual) or not translated at all (Missing). Defaults to all.

.PARAMETER Keys
Only include these resource keys.

.PARAMETER MaxUsages
Maximum number of code usages to show per key. 0 omits usages (e.g. for a glossary dump).

.PARAMETER OutFile
Writes the output to this file (UTF-8) instead of the pipeline.
#>
param(
    [string[]]$Languages,
    [string[]]$Projects,
    [ValidateSet('All', 'Auto', 'Manual', 'Missing')][string]$Status = 'All',
    [string[]]$Keys,
    [int]$MaxUsages = 3,
    [string]$OutFile
)
. "$PSScriptRoot\ResxHelpers.ps1"

# Index all references to Resources.<Key> in C# code (excluding generated designer files)
$usages = @{}
Get-ChildItem "$RepoRoot\src" -Recurse -Filter *.cs |
    Where-Object { $_.Name -notlike '*.Designer.cs' -and $_.FullName -notmatch '\\(bin|obj)\\' } |
    ForEach-Object {
        $file = Get-RelativePath $_.FullName
        $lines = [IO.File]::ReadAllLines($_.FullName)
        for ($i = 0; $i -lt $lines.Length; $i++) {
            foreach ($match in [regex]::Matches($lines[$i], '\bResources\.(\w+)')) {
                $key = $match.Groups[1].Value
                if (-not $usages.ContainsKey($key)) { $usages[$key] = New-Object System.Collections.ArrayList }
                $from = [Math]::Max(0, $i - 1)
                $to = [Math]::Min($lines.Length - 1, $i + 1)
                [void]$usages[$key].Add([pscustomobject]@{
                    File = $file
                    Line = $i + 1
                    Snippet = ($lines[$from..$to] | ForEach-Object { '    | ' + $_.Trim() }) -join "`n"
                })
            }
        }
    }

function Get-StatusLabel($Entry) {
    if (-not $Entry -or -not $Entry.Value) { 'missing' }
    elseif ($Entry.Comment -eq $ManualComment) { 'manual' }
    elseif ($Entry.Comment -eq $AutoComment) { 'auto' }
    else { 'unknown' }
}

$output = foreach ($project in Get-ResxProjects $Projects) {
    $source = Read-ResxFile $project.Source
    $projectPrefix = "src/$($project.Name)/"
    $translations = @(Get-LocalizedResxFiles $project $Languages | ForEach-Object {
        [pscustomobject]@{ Language = $_.Language; Entries = Read-ResxFile $_.Path }
    })

    foreach ($key in $source.Keys) {
        if ($Keys -and $Keys -notcontains $key) { continue }

        $rows = foreach ($translation in $translations) {
            $entry = $translation.Entries[$key]
            $label = Get-StatusLabel $entry
            if ($Status -eq 'All' -or $Status -eq $label) {
                "$($translation.Language) [$label]: $(if ($entry) { $entry.Value })"
            }
        }
        if (-not $rows) { continue }

        "## $($project.Name): $key"
        "en: $($source[$key].Value)"
        $keyUsages = if ($usages.ContainsKey($key)) {
            # Prefer usages in the same project and non-test code
            $usages[$key] |
                Sort-Object @{ Expression = { -not $_.File.StartsWith($projectPrefix) } }, @{ Expression = { $_.File -like '*Tests*' } }, File, Line |
                Select-Object -First $MaxUsages
        }
        if ($MaxUsages -le 0) {
            # Usages not requested (e.g. for a glossary dump)
        } elseif ($keyUsages) {
            'used:'
            foreach ($usage in $keyUsages) {
                "  $($usage.File):$($usage.Line)"
                $usage.Snippet
            }
            if ($usages[$key].Count -gt $MaxUsages) { "  (+$($usages[$key].Count - $MaxUsages) more)" }
        } else {
            'used: (no direct references found)'
        }
        $rows
        ''
    }
}

if ($OutFile) {
    [IO.File]::WriteAllText($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutFile),($output -join "`n"), $Utf8NoBom)
    Write-Host "Wrote $(@($output | Where-Object { $_ -like '## *' }).Count) keys to $OutFile"
} else {
    $output
}
