[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$modsRoot = Split-Path -Parent $modRoot
$project = Join-Path $modRoot 'GlobalLedger.csproj'
& (Join-Path $modsRoot '_AI\_Workflows\Tools\SourceLayout\Check-SourceLayout.ps1') -ModFolder $modRoot
& dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Global Ledger build failed.' }
$release = Join-Path $modRoot 'Release'
New-Item -ItemType Directory -Path $release -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'bin\Release\netstandard2.1\GlobalLedger.dll') -Destination $release -Force
Copy-Item -LiteralPath (Join-Path $modRoot 'mod.json') -Destination $release -Force
$metadata = Get-Content -LiteralPath (Join-Path $modRoot 'mod.json') -Raw | ConvertFrom-Json
if ($metadata.version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a numeric mod version.' }
$archive = Join-Path $release ('GlobalLedger-v' + $metadata.version + '.zip')
Add-Type -AssemblyName System.IO.Compression
$stream = [IO.File]::Open($archive, [IO.FileMode]::Create)
$zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in @('GlobalLedger.dll', 'mod.json')) {
        $entry = $zip.CreateEntry(('BepInEx/plugins/GlobalLedger/' + $name))
        $target = $entry.Open()
        $source = [IO.File]::OpenRead((Join-Path $release $name))
        try { $source.CopyTo($target) } finally { $source.Dispose(); $target.Dispose() }
    }
}
finally { $zip.Dispose(); $stream.Dispose() }
Write-Host "Local package: $archive"
Write-Host 'Nothing installed or published.'
