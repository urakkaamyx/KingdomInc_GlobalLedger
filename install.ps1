param(
    [switch]$WhenGameCloses,
    [string]$PendingDll,
    [string]$ExpectedSha256
)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$source = if ($PendingDll) { $PendingDll } else { Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\GlobalLedger.dll' }
if ($PendingDll) {
    if (!$ExpectedSha256 -or (Get-FileHash -LiteralPath $source).Hash -ne $ExpectedSha256) { throw 'Pending DLL hash verification failed.' }
    do {
        $running = @(Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
        foreach ($game in $running) { try { $game.WaitForExit() } catch { if (!$game.HasExited) { throw } } }
    } while (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
} else {
    $builtFoundation = Join-Path $PSScriptRoot '..\Foundation\bin\Release\netstandard2.1\Foundation.dll'
    $foundation = if (Test-Path -LiteralPath $builtFoundation) { $builtFoundation } else { Join-Path $gameRoot 'BepInEx\plugins\Foundation\Foundation.dll' }
    if (!(Test-Path -LiteralPath $foundation) -or [Reflection.AssemblyName]::GetAssemblyName($foundation).Version -lt [Version]'0.6.0') {
        throw 'Install Foundation 0.6.0 or later first.'
    }
    dotnet build (Join-Path $PSScriptRoot 'GlobalLedger.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Global Ledger build failed.' }
}
$gameRunning = [bool](Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
if ($gameRunning -and $WhenGameCloses) {
    $pending = Join-Path $PSScriptRoot ('Releases\Pending-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $pending -Force | Out-Null
    $snapshot = Join-Path $pending 'GlobalLedger.dll'
    Copy-Item -LiteralPath $source -Destination $snapshot
    $digest = (Get-FileHash -LiteralPath $snapshot).Hash
    $shellExe = (Get-Process -Id $PID).Path
    $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"' + $PSCommandPath + '"'),'-PendingDll',('"' + $snapshot + '"'),'-ExpectedSha256',$digest)
    $helper = Start-Process -FilePath $shellExe -WindowStyle Hidden -ArgumentList $arguments -RedirectStandardOutput (Join-Path $pending 'install.log') -RedirectStandardError (Join-Path $pending 'install-error.log') -PassThru
    [pscustomobject]@{ Status = 'Queued until Kingdom Inc closes'; HelperPID = $helper.Id; PendingDll = $snapshot; SHA256 = $digest } | ConvertTo-Json
    return
}
if ($gameRunning) { throw 'Close Kingdom Inc before installing Global Ledger, or pass -WhenGameCloses.' }
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
Copy-Item -LiteralPath $source -Destination $dll -Force
Get-FileHash -LiteralPath $dll -Algorithm SHA256
