param([Parameter(Mandatory=$true)][ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunName)
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$gameRoot = [IO.Path]::GetFullPath((Join-Path $modRoot '..\..'))
$runtime = Join-Path $modRoot ('_AI\Scratch\Runtime-' + $RunName)
if (Test-Path -LiteralPath $runtime) { throw "Verification runtime already exists: $runtime" }
$sources = @(
    (Join-Path $gameRoot '_Mods\Foundation\Release\Foundation.dll'),
    (Join-Path $modRoot 'Release\GlobalLedger.dll'),
    (Join-Path $gameRoot '_Mods\RightClickFix\Release\RightClickFix.dll'),
    (Join-Path $modRoot '_AI\Scratch\Verification\bin\netstandard2.1\GlobalLedger.Verification.dll')
)
foreach ($source in $sources) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing verification input: $source" }
}
New-Item -ItemType Directory -Path $runtime,(Join-Path $runtime 'BepInEx\core'),(Join-Path $runtime 'BepInEx\plugins'),(Join-Path $runtime 'BepInEx\config') -Force | Out-Null
foreach ($file in @('KingdomInc.exe','UnityPlayer.dll','winhttp.dll','doorstop_config.ini','.doorstop_version')) {
    Copy-Item -LiteralPath (Join-Path $gameRoot $file) -Destination $runtime
}
foreach ($name in @('KingdomInc_Data','MonoBleedingEdge')) {
    New-Item -ItemType Junction -Path (Join-Path $runtime $name) -Target (Join-Path $gameRoot $name) | Out-Null
}
Get-ChildItem -LiteralPath (Join-Path $gameRoot 'BepInEx\core') -File -Filter '*.dll' | Copy-Item -Destination (Join-Path $runtime 'BepInEx\core')
foreach ($source in $sources) { Copy-Item -LiteralPath $source -Destination (Join-Path $runtime 'BepInEx\plugins') }
Set-Content -LiteralPath (Join-Path $runtime 'BepInEx\config\BepInEx.cfg') -Value "[Logging.Console]`nEnabled = false`n"
$inputs = foreach ($source in $sources) {
    [pscustomobject]@{ Path=$source; Version=[Reflection.AssemblyName]::GetAssemblyName($source).Version.ToString(); SHA256=(Get-FileHash -LiteralPath $source).Hash }
}
$inputs | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runtime 'inputs.json')
$arguments = @('-global-ledger-v1-verification','-ledger-visual-verification','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile','unity.log')
$process = Start-Process -FilePath (Join-Path $runtime 'KingdomInc.exe') -WorkingDirectory $runtime -WindowStyle Hidden -ArgumentList $arguments -PassThru
[pscustomobject]@{ ProcessId=$process.Id; Runtime=$runtime; Results=(Join-Path $runtime 'verification-results\results.json') } | ConvertTo-Json
