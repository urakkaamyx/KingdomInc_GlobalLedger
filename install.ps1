param()
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before installing the prototype.' }
$foundation = Join-Path $gameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
if (!(Test-Path -LiteralPath $foundation) -or [Reflection.AssemblyName]::GetAssemblyName($foundation).Version -lt [Version]'0.6.0') {
    throw 'Install Foundation 0.6.0 or later first.'
}
dotnet build (Join-Path $PSScriptRoot 'GlobalLedger.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Global Ledger build failed.' }
$target = Join-Path $gameRoot 'BepInEx\plugins\GlobalLedger'
New-Item -ItemType Directory -Path $target -Force | Out-Null
$dll = Join-Path $target 'GlobalLedger.dll'
if (Test-Path -LiteralPath $dll) {
    $backup = Join-Path $PSScriptRoot ('Backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-ffff'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Copy-Item -LiteralPath $dll -Destination (Join-Path $backup 'GlobalLedger.dll')
}
# Check again after building; never replace an assembly mapped by a running game.
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Kingdom Inc started during the build. Close it and run install.ps1 again.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\GlobalLedger.dll') -Destination $dll -Force
Get-FileHash -LiteralPath $dll -Algorithm SHA256
